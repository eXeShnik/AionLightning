using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Player selects an option from an NPC dialog. Opcode 0x114.</summary>
public sealed class CM_DIALOG_SELECT : AionClientPacket
{
    // Dialog action IDs from Java DialogAction enum
    private const int RETRIEVE_ACCOUNT_WH  = 27;
    private const int DEPOSIT_ACCOUNT_WH   = 28;
    private const int BUY                  = 2;
    private const int TRADE_SELL_LIST      = 103;
    private const int WAREHOUSE_OPEN       = 26;
    private const int AIRLINE_SERVICE      = 44;
    private const int RESURRECT_BIND       = 34;
    private const int QUEST_SELECT         = 31;
    private const int OPEN_POSTBOX         = 38;
    private const int OPEN_VENDOR          = 33;
    private const int QUEST_ACCEPT         = 29;
    private const int QUEST_ACCEPT_1       = 1002;
    private const int SELECT_QUEST_REWARD  = 1009;
    private const int EXTEND_INVENTORY     = 47;
    private const int OPEN_STIGMA_WINDOW   = 4;
    private const int GATHER_SKILL_LEVELUP    = 45;
    private const int COMBINE_SKILL_LEVELUP   = 46;
    private const int RECOVERY                = 35;
    private const int OPEN_LEGION_WAREHOUSE   = 53;
    private const int BUY_AGAIN               = 70; // repurchase previously sold items from NPC

    // Kinah cost per soul sickness stack when recovering at a Healer NPC
    private const long SoulSicknessCostPerStack = 5_000;

    // Tier costs for craft/gathering skill upgrades (Java CraftSkillUpdateService cost map).
    // Key = current skill level; value = kinah price to advance to the next tier.
    private static readonly Dictionary<int, long> CraftTierCosts = new()
    {
        [0]   = 3_500,       // learn basic (level 0 → 1)
        [99]  = 17_000,      // apprentice (99 → 100)
        [199] = 115_000,     // journeyman (199 → 200)
        [299] = 460_000,     // expert unlock (299 → 300)
        [399] = -1,          // expert cap — requires quest; -1 = blocked
        [449] = 6_004_900,   // gathering advanced (449 → 450)
        [499] = -1,          // master cap — requires quest; -1 = blocked
    };

    // NPC ID → (skillId, skillName). Mirrors Java CraftSkillUpdateService.npcBySkill.
    private static readonly Dictionary<int, (int SkillId, string Name)> NpcSkillMap = new()
    {
        // Asmodian gatherers
        [204096] = (30002, "Extract Vitality"), [830158] = (30002, "Extract Vitality"),
        [204257] = (30003, "Extract Aether"),   [830148] = (30003, "Extract Aether"),
        // Asmodian crafters
        [204100] = (40001, "Cooking"),          [830142] = (40001, "Cooking"),
        [204104] = (40002, "Weaponsmithing"),   [830146] = (40002, "Weaponsmithing"),
        [204106] = (40003, "Armorsmithing"),    [830144] = (40003, "Armorsmithing"),
        [204110] = (40004, "Tailoring"),        [830136] = (40004, "Tailoring"),
        [204102] = (40007, "Alchemy"),          [830138] = (40007, "Alchemy"),
        [204108] = (40008, "Handicrafting"),    [830140] = (40008, "Handicrafting"),
        [798452] = (40010, "Menusier"),         [798456] = (40010, "Menusier"),
        // Elyos gatherers
        [203780] = (30002, "Extract Vitality"), [830066] = (30002, "Extract Vitality"),
        [203782] = (30003, "Extract Aether"),   [830064] = (30003, "Extract Aether"),
        // Elyos crafters
        [203784] = (40001, "Cooking"),          [830058] = (40001, "Cooking"),
        [203788] = (40002, "Weaponsmithing"),   [830062] = (40002, "Weaponsmithing"),
        [203790] = (40003, "Armorsmithing"),    [830060] = (40003, "Armorsmithing"),
        [203793] = (40004, "Tailoring"),        [830052] = (40004, "Tailoring"),
        [203786] = (40007, "Alchemy"),          [830054] = (40007, "Alchemy"),
        [203792] = (40008, "Handicrafting"),    [830056] = (40008, "Handicrafting"),
        [798450] = (40010, "Menusier"),         [798454] = (40010, "Menusier"),
    };

    // Crafting skill IDs (vs gathering). Max 2 expert (≥400) crafting skills, max 1 master (≥500).
    private static readonly HashSet<int> CraftingSkillIds = [40001, 40002, 40003, 40004, 40007, 40008, 40010];

    private const int   KinahItemId      = 182400001;
    private const float MaxInteractRange = 10.0f;

    private readonly GsClientConnection        _conn;
    private readonly GameWorld                 _world;
    private readonly IDataManager              _dataManager;
    private readonly IQuestDao                 _questDao;
    private readonly IItemDao                  _itemDao;
    private readonly IPlayerDao                _playerDao;
    private readonly IMailDao                  _mailDao;
    private readonly SkillLearnService         _skillLearn;
    private readonly ILegionDao                _legionDao;
    private readonly PlayerConnectionRegistry  _connRegistry;
    private readonly RepurchaseService         _repurchaseService;
    private readonly QuestEngineType _questEngine;
    private readonly QuestRewardService        _questRewardService;
    private readonly ILogger<CM_DIALOG_SELECT> _log;

    private int _targetObjectId;
    private int _dialogId;
    private int _rewardIndex;
    private int _questId;

    public CM_DIALOG_SELECT(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, IQuestDao questDao, IItemDao itemDao, IPlayerDao playerDao,
        IMailDao mailDao, SkillLearnService skillLearn, ILegionDao legionDao,
        PlayerConnectionRegistry connRegistry, RepurchaseService repurchaseService,
        QuestEngineType questEngine, QuestRewardService questRewardService,
        ILogger<CM_DIALOG_SELECT> log)
    {
        _conn                = conn;
        _world               = world;
        _dataManager         = dataManager;
        _questDao            = questDao;
        _itemDao             = itemDao;
        _playerDao           = playerDao;
        _mailDao             = mailDao;
        _skillLearn          = skillLearn;
        _legionDao           = legionDao;
        _connRegistry        = connRegistry;
        _repurchaseService   = repurchaseService;
        _questEngine         = questEngine;
        _questRewardService  = questRewardService;
        _log                 = log;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        _dialogId       = r.ReadH();
        _rewardIndex    = r.ReadH();
        r.ReadH();       // lastPage
        _questId        = r.ReadD();
    }

    // Dialog IDs mapping class selection choices → new PlayerClass (matches Java ClassChangeService)
    private static readonly Dictionary<(Race, int), PlayerClass> ClassChoiceMap = new()
    {
        // Elyos choices
        { (Race.ELYOS, 2376), PlayerClass.GLADIATOR    },
        { (Race.ELYOS, 2461), PlayerClass.TEMPLAR       },
        { (Race.ELYOS, 2717), PlayerClass.ASSASSIN      },
        { (Race.ELYOS, 2802), PlayerClass.RANGER        },
        { (Race.ELYOS, 3058), PlayerClass.SORCERER      },
        { (Race.ELYOS, 3143), PlayerClass.SPIRIT_MASTER },
        { (Race.ELYOS, 3399), PlayerClass.CLERIC        },
        { (Race.ELYOS, 3484), PlayerClass.CHANTER       },
        // Asmodian choices
        { (Race.ASMODIANS, 3058), PlayerClass.GLADIATOR    },
        { (Race.ASMODIANS, 3143), PlayerClass.TEMPLAR       },
        { (Race.ASMODIANS, 3399), PlayerClass.ASSASSIN      },
        { (Race.ASMODIANS, 3484), PlayerClass.RANGER        },
        { (Race.ASMODIANS, 3740), PlayerClass.SORCERER      },
        { (Race.ASMODIANS, 3825), PlayerClass.SPIRIT_MASTER },
        { (Race.ASMODIANS, 4081), PlayerClass.CLERIC        },
        { (Race.ASMODIANS, 4166), PlayerClass.CHANTER       },
    };

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // targetObjectId == 0 means the dialog is from the class-change UI, not an NPC
        if (_targetObjectId == 0)
        {
            if (ClassChoiceMap.TryGetValue((player.Race, _dialogId), out var newClass))
                await HandleClassChangeAsync(player, newClass, ct);
            return;
        }

        switch (_dialogId)
        {
            case BUY:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null) return;
                if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                var goodsListIds = _dataManager.Shop.GetGoodsListIds(npc.Template.NpcId);
                if (goodsListIds is { Count: > 0 })
                    await _conn.SendAsync(new SM_TRADELIST(_targetObjectId, goodsListIds), ct);
                else
                    await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 0), ct);
                break;
            }

            case TRADE_SELL_LIST:
                // Java DialogService SELL case — opens the vendor sell window
                await _conn.SendAsync(new SM_SELL_ITEM(_targetObjectId), ct);
                break;

            case WAREHOUSE_OPEN:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
                break;
            }

            case AIRLINE_SERVICE:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null) return;
                if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                var tpId = _dataManager.Teleports.GetTeleportId(npc.Template.NpcId);
                if (tpId is null) return;
                await _conn.SendAsync(new SM_TELEPORT_MAP(_targetObjectId, tpId.Value), ct);
                break;
            }

            case RESURRECT_BIND:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                player.BindPosition = npc.Position;
                await _playerDao.UpdateBindPointAsync(player.ObjectId, npc.Position, ct);
                await _conn.SendAsync(new SM_BIND_POINT_INFO(npc.Position), ct);
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.BindPointSet(), ct);
                break;
            }

            case RETRIEVE_ACCOUNT_WH:
            case DEPOSIT_ACCOUNT_WH:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                await _conn.SendAsync(new SM_ACCOUNT_WAREHOUSE_INFO(player.AccountWarehouse.All), ct);
                break;
            }

            case QUEST_SELECT:
                await HandleQuestSelectAsync(player, ct);
                break;

            case OPEN_POSTBOX:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                // dialogId 18 = mail window in the client dialog UI
                await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 18), ct);
                var mails = await _mailDao.GetReceivedMailsAsync(player.ObjectId, ct);
                await _conn.SendAsync(new SM_MAIL_SERVICE(player.ObjectId, mails), ct);
                break;
            }

            case OPEN_VENDOR:
                // dialogId 13 = consignment trade window
                await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 13), ct);
                break;

            case QUEST_ACCEPT:
            case QUEST_ACCEPT_1:
                await HandleQuestAcceptAsync(player, ct);
                break;

            case SELECT_QUEST_REWARD:
                await HandleQuestRewardAsync(player, ct);
                break;

            case EXTEND_INVENTORY:
                await HandleExpandCubeAsync(player, ct);
                break;

            case OPEN_STIGMA_WINDOW:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 1), ct);
                break;
            }

            case GATHER_SKILL_LEVELUP:
            case COMBINE_SKILL_LEVELUP:
                await HandleCraftSkillUpgradeAsync(player, ct);
                break;

            case RECOVERY:
                await HandleSoulSicknessRecoveryAsync(player, ct);
                break;

            case OPEN_LEGION_WAREHOUSE:
                await HandleOpenLegionWarehouseAsync(player, ct);
                break;

            case BUY_AGAIN:
            {
                var npc = _world.GetNpcByObjectId(_targetObjectId);
                if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;
                var entries = _repurchaseService.GetAll(player.ObjectId);
                await _conn.SendAsync(new SM_REPURCHASE(_targetObjectId, entries), ct);
                break;
            }

            default:
                _log.LogDebug("Unhandled dialog: targetId={TargetId} dialogId={DialogId} questId={QuestId}",
                    _targetObjectId, _dialogId, _questId);
                break;
        }
    }

    private async ValueTask HandleClassChangeAsync(Model.Player player, PlayerClass newClass, CancellationToken ct)
    {
        // Guard: only starting classes at level 9 may ascend
        if (!player.PlayerClass.IsStartingClass()) return;
        if (player.Level < 9) return;

        // Validate the new class is a valid ascension for the starting class
        bool valid = (player.PlayerClass, newClass) switch
        {
            (PlayerClass.WARRIOR, PlayerClass.GLADIATOR or PlayerClass.TEMPLAR)           => true,
            (PlayerClass.SCOUT,   PlayerClass.ASSASSIN  or PlayerClass.RANGER)            => true,
            (PlayerClass.MAGE,    PlayerClass.SORCERER  or PlayerClass.SPIRIT_MASTER)     => true,
            (PlayerClass.PRIEST,  PlayerClass.CLERIC    or PlayerClass.CHANTER)           => true,
            _ => false,
        };
        if (!valid) return;

        var oldClass = player.PlayerClass;
        player.PlayerClass = newClass;
        await _playerDao.UpdateClassAsync(player.ObjectId, newClass, ct);

        // Grant all skills for the new class from level 1 to current level (addMissingSkills), then persist.
        _skillLearn.ApplyAutoLearn(player, 1, player.Level);
        await _skillLearn.PersistAllAsync(player, ct);

        // Refresh stats for the new class
        var tpl = _dataManager.PlayerStats.GetTemplate(newClass, player.Level);
        if (tpl is not null)
        {
            player.MaxHp     = (int)((tpl.MaxHp + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * player.SoulSicknessMultiplier);
            player.MaxMp     = (int)((tpl.MaxMp + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * player.SoulSicknessMultiplier);
            player.CurrentHp = player.MaxHp;
            player.CurrentMp = player.MaxMp;
            player.BasePhysicalAttack = tpl.MainHandAttack;
        }

        await _conn.SendAsync(new SM_DIALOG_WINDOW(0, 0, 0), ct); // close the class selection UI
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills, isNew: true), ct);

        _log.LogInformation("Player {Name} ascended from {Old} to {New}", player.Name, oldClass, newClass);
    }

    private async ValueTask HandleQuestSelectAsync(Model.Player player, CancellationToken ct)
    {
        if (_questId <= 0) return;

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        var env = new QuestEnv(npc, player, _questId, QUEST_SELECT);
        if (await _questEngine.OnDialogAsync(env, _conn, ct)) return;

        var template = _dataManager.Quests.GetTemplate(_questId);
        if (template is null) return;

        var entry = player.Quests.Get(_questId);
        if (entry is not null)
        {
            // Quest already started: show in-progress dialog; REWARD state shows the turn-in page
            int dlg = entry.Status == QuestStatus.REWARD ? 1352 : 2375;
            await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dlg, _questId), ct);
            return;
        }

        if (player.Level < template.MinLevel) return;
        if (template.Race != "PC_ALL" && !string.Equals(template.Race, player.Race.ToString(), StringComparison.OrdinalIgnoreCase))
            return;

        // Show quest description with Accept/Decline buttons
        await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, 1007, _questId), ct);
    }

    private async ValueTask HandleQuestAcceptAsync(Model.Player player, CancellationToken ct)
    {
        if (_questId <= 0) return;
        if (player.Quests.Contains(_questId)) return;

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        var env = new QuestEnv(npc, player, _questId, _dialogId);
        if (await _questEngine.OnDialogAsync(env, _conn, ct)) return;

        var template = _dataManager.Quests.GetTemplate(_questId);
        if (template is null) return;
        if (player.Level < template.MinLevel) return;

        // Race restriction: "PC_ALL" means any race; otherwise must match player's race
        if (template.Race != "PC_ALL" && !string.Equals(template.Race, player.Race.ToString(), StringComparison.OrdinalIgnoreCase))
            return;

        var entry = new QuestEntry { QuestId = _questId, Status = QuestStatus.START };
        player.Quests.Add(entry);
        await _questDao.UpsertAsync(player.ObjectId, entry, ct);

        await _conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        _log.LogInformation("Player {Name} accepted quest {QuestId} ({QuestName})",
            player.Name, _questId, template.Name);
    }

    private async ValueTask HandleQuestRewardAsync(Model.Player player, CancellationToken ct)
    {
        if (_questId <= 0) return;

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        var env = new QuestEnv(npc, player, _questId, SELECT_QUEST_REWARD, _rewardIndex);
        if (await _questEngine.OnDialogAsync(env, _conn, ct)) return;

        var entry = player.Quests.Get(_questId);
        if (entry is null || entry.Status == QuestStatus.COMPLETE) return;

        var template = _dataManager.Quests.GetTemplate(_questId);
        if (template is null) return;

        await _questRewardService.GrantAndCompleteAsync(_conn, player, entry, template, _rewardIndex, ct);
    }

    private async ValueTask HandleExpandCubeAsync(Model.Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        if (!_dataManager.CubeExpander.IsCubeExpander(npc.Template.NpcId)) return;

        int nextLevel = player.NpcExpands + 1;
        long? price   = _dataManager.CubeExpander.GetExpandPrice(npc.Template.NpcId, nextLevel);
        if (price is null)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CannotExpandCubeMore(), ct);
            return;
        }

        var  kinah   = player.Inventory.FindByItemId(KinahItemId);
        long current = kinah?.Count ?? 0;
        if (current < price.Value)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        // Deduct kinah, expand cube
        kinah!.Count         -= price.Value;
        player.NpcExpands++;
        player.Inventory.Capacity = player.CubeCapacity;

        await _playerDao.UpdateCubeExpandAsync(player.ObjectId, player.NpcExpands, ct);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        await _conn.SendAsync(SM_CUBE_UPDATE.CubeSize(
            player.Inventory.BagSlotUsed, player.NpcExpands, player.QuestExpands), ct);

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.CubeExpanded(9), ct);

        _log.LogInformation("Player {Name} expanded cube to {Slots} slots (npcExpands={Level})",
            player.Name, player.CubeCapacity, player.NpcExpands);
    }

    private async ValueTask HandleCraftSkillUpgradeAsync(Model.Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        if (player.Level < 10) return;

        if (!NpcSkillMap.TryGetValue(npc.Template.NpcId, out var skillInfo)) return;
        int skillId   = skillInfo.SkillId;
        string skillName = skillInfo.Name;

        int currentLevel = player.Skills.GetLevel(skillId);

        if (!CraftTierCosts.TryGetValue(currentLevel, out long cost))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CraftSkillMaxLevel(), ct);
            return;
        }

        // -1 means this tier requires quest completion
        if (cost < 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CraftSkillNeedQuest(), ct);
            return;
        }

        // Gathering skills (30002/30003) have no upgrade at level 449
        if (currentLevel == 449 && (skillId == 30002 || skillId == 30003))
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.CraftSkillMaxLevel(), ct);
            return;
        }

        // Crafting cap: max 2 expert (≥400), max 1 master (≥500)
        if (CraftingSkillIds.Contains(skillId))
        {
            if (currentLevel == 399)
            {
                int expertCount = CountCraftingSkillsAbove(player, 399);
                if (expertCount >= 2)
                {
                    await _conn.SendAsync(SM_SYSTEM_MESSAGE.CraftSkillMaxLevel(), ct);
                    return;
                }
            }
            else if (currentLevel == 499)
            {
                int masterCount = CountCraftingSkillsAbove(player, 499);
                if (masterCount >= 1)
                {
                    await _conn.SendAsync(SM_SYSTEM_MESSAGE.CraftSkillMaxLevel(), ct);
                    return;
                }
            }
        }

        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null || kinah.Count < cost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        kinah.Count -= cost;
        int newLevel = currentLevel + 1;
        await _skillLearn.LearnSkillAsync(player, skillId, newLevel, ct: ct);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);

        var upgraded = player.Skills.GetEntry(skillId)!;
        await _conn.SendAsync(new SM_SKILL_LIST([upgraded], isNew: true, msgId: 1330004, skillName: skillName, skillLevel: newLevel), ct);

        _log.LogInformation("Player {Name} upgraded {Skill} to level {Level}", player.Name, skillName, newLevel);
    }

    private async ValueTask HandleOpenLegionWarehouseAsync(Model.Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        var legion = player.Legion;
        if (legion is null) return;

        await _conn.SendAsync(SM_LEGION_EDIT.WarehouseKinah(legion.WarehouseKinah), ct);
        await _conn.SendAsync(new SM_LEGION_WAREHOUSE_INFO(legion.WarehouseItems.All), ct);
        await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 25), ct);
    }

    private static int CountCraftingSkillsAbove(Model.Player player, int threshold)
    {
        int count = 0;
        foreach (int id in CraftingSkillIds)
        {
            int lvl = player.Skills.GetLevel(id);
            if (lvl > threshold) count++;
        }
        return count;
    }

    private async ValueTask HandleSoulSicknessRecoveryAsync(Model.Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        if (player.SoulSicknessCount == 0) return;

        long cost = SoulSicknessCostPerStack * player.SoulSicknessCount;
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if ((kinahItem?.Count ?? 0) < cost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        kinahItem!.Count -= cost;
        player.SoulSicknessCount = 0;
        await _playerDao.UpdateSoulSicknessAsync(player.ObjectId, 0, ct);

        // Recompute MaxHp/MaxMp without soul sickness penalty
        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        if (statTpl is not null)
        {
            player.MaxHp = statTpl.MaxHp + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp;
            player.MaxMp = statTpl.MaxMp + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp;
        }

        // Remove soul sickness skull icon from client
        player.RemoveEffectBySkillId(8291);
        int ssWorld  = player.Position.WorldId;
        var ssEffect = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        foreach (var c in _connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == ssWorld)
                try { await c.SendAsync(ssEffect, ct); } catch { }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.SoulSicknessCleared(), ct);
    }
}
