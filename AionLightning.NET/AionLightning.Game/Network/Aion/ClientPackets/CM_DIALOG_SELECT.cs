using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Player selects an option from an NPC dialog. Opcode 0x114.</summary>
public sealed class CM_DIALOG_SELECT : AionClientPacket
{
    // Dialog action IDs from Java DialogAction enum
    private const int BUY                 = 2;
    private const int TRADE_SELL_LIST     = 103;
    private const int WAREHOUSE_OPEN      = 26;
    private const int AIRLINE_SERVICE     = 44;
    private const int RESURRECT_BIND      = 34;
    private const int QUEST_ACCEPT        = 29;
    private const int QUEST_ACCEPT_1      = 1002;
    private const int SELECT_QUEST_REWARD = 1009;

    private const int   KinahItemId      = 182400001;
    private const float MaxInteractRange = 10.0f;

    private readonly GsClientConnection        _conn;
    private readonly GameWorld                 _world;
    private readonly IDataManager              _dataManager;
    private readonly IQuestDao                 _questDao;
    private readonly IItemDao                  _itemDao;
    private readonly IPlayerDao                _playerDao;
    private readonly ExperienceService         _expService;
    private readonly PlayerConnectionRegistry  _connRegistry;
    private readonly ILogger<CM_DIALOG_SELECT> _log;

    private int _targetObjectId;
    private int _dialogId;
    private int _rewardIndex;
    private int _questId;

    public CM_DIALOG_SELECT(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, IQuestDao questDao, IItemDao itemDao, IPlayerDao playerDao,
        ExperienceService expService, PlayerConnectionRegistry connRegistry,
        ILogger<CM_DIALOG_SELECT> log)
    {
        _conn         = conn;
        _world        = world;
        _dataManager  = dataManager;
        _questDao     = questDao;
        _itemDao      = itemDao;
        _playerDao    = playerDao;
        _expService   = expService;
        _connRegistry = connRegistry;
        _log          = log;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        _dialogId       = r.ReadH();
        _rewardIndex    = r.ReadH();
        r.ReadH();       // lastPage
        _questId        = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

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
                await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 21), ct);
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

            case QUEST_ACCEPT:
            case QUEST_ACCEPT_1:
                await HandleQuestAcceptAsync(player, ct);
                break;

            case SELECT_QUEST_REWARD:
                await HandleQuestRewardAsync(player, ct);
                break;

            default:
                _log.LogDebug("Unhandled dialog: targetId={TargetId} dialogId={DialogId} questId={QuestId}",
                    _targetObjectId, _dialogId, _questId);
                break;
        }
    }

    private async ValueTask HandleQuestAcceptAsync(Model.Player player, CancellationToken ct)
    {
        if (_questId <= 0) return;
        if (player.Quests.Contains(_questId)) return;

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

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

        var entry = player.Quests.Get(_questId);
        if (entry is null || entry.Status == QuestStatus.COMPLETE) return;

        var template = _dataManager.Quests.GetTemplate(_questId);
        if (template is null) return;

        // When status is START, validate objectives here (REWARD state was already validated at transition)
        if (entry.Status == QuestStatus.START)
        {
            foreach (var kill in template.QuestKills)
                if (entry.GetVar(kill.Seq) < kill.Count) return;

            if (template.CollectItems is { Items.Count: > 0 })
                foreach (var req in template.CollectItems.Items)
                {
                    var chk = player.Inventory.FindByItemId(req.ItemId);
                    if (chk is null || chk.Count < req.Count) return;
                }
        }

        // Validate and consume collect_item requirements
        if (template.CollectItems is { Items.Count: > 0 })
        {
            foreach (var req in template.CollectItems.Items)
            {
                var item = player.Inventory.FindByItemId(req.ItemId);
                if (item is null || item.Count < req.Count) return;
            }

            var partiallyConsumed = new List<Model.Item.Item>();
            foreach (var req in template.CollectItems.Items)
            {
                var item = player.Inventory.FindByItemId(req.ItemId);
                if (item is null) continue;
                item.Count -= req.Count;
                if (item.Count <= 0)
                {
                    player.Inventory.Remove(item.UniqueId);
                    await _itemDao.DeleteAsync(item.UniqueId, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
                }
                else
                {
                    partiallyConsumed.Add(item);
                }
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            if (partiallyConsumed.Count > 0)
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumed), ct);
        }

        // Award experience
        long expReward = template.Rewards?.Exp ?? 0;
        if (expReward > 0)
            await _expService.AddExpAsync(player, expReward, _conn, ct);

        // Award selected reward item
        var selectableItems = template.Rewards?.SelectableItems;
        if (selectableItems is { Count: > 0 } && _rewardIndex >= 0 && _rewardIndex < selectableItems.Count)
        {
            var reward  = selectableItems[_rewardIndex];
            int maxStk  = _dataManager.Items.GetTemplate(reward.ItemId)?.MaxStackCount ?? 1;
            if (player.Inventory.CanReceive(reward.ItemId, maxStk))
            {
                var existed = player.Inventory.FindByItemId(reward.ItemId);
                Item rewardItem;
                if (existed is not null)
                {
                    existed.Count += reward.Count;
                    rewardItem = existed;
                }
                else
                {
                    long uid = await _itemDao.NextUniqueIdAsync(ct);
                    rewardItem = new Item { UniqueId = uid, ItemId = reward.ItemId, Count = reward.Count, Slot = -1 };
                    player.Inventory.Add(rewardItem);
                }
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([rewardItem]), ct);
            }
        }

        // Award fixed reward items (always given, no selection)
        var fixedItems = template.Rewards?.RewardItems;
        if (fixedItems is { Count: > 0 })
        {
            var granted = new List<Item>();
            foreach (var reward in fixedItems)
            {
                int maxStk = _dataManager.Items.GetTemplate(reward.ItemId)?.MaxStackCount ?? 1;
                if (!player.Inventory.CanReceive(reward.ItemId, maxStk)) continue;

                var existed = player.Inventory.FindByItemId(reward.ItemId);
                if (existed is not null)
                {
                    existed.Count += reward.Count;
                    granted.Add(existed);
                }
                else
                {
                    long uid = await _itemDao.NextUniqueIdAsync(ct);
                    var item = new Item { UniqueId = uid, ItemId = reward.ItemId, Count = reward.Count, Slot = -1 };
                    player.Inventory.Add(item);
                    granted.Add(item);
                }
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            if (granted.Count > 0)
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(granted), ct);
        }

        // Award kinah (gold attribute)
        long gold = template.Rewards?.Gold ?? 0;
        if (gold > 0)
        {
            var kinah = player.Inventory.FindByItemId(KinahItemId);
            if (kinah is not null)
            {
                kinah.Count += gold;
            }
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                kinah = new Item { UniqueId = uid, ItemId = KinahItemId, Count = gold, Slot = -1 };
                player.Inventory.Add(kinah);
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        }

        // Award abyss points
        int apReward = template.Rewards?.RewardAbyssPoint ?? 0;
        if (apReward > 0)
        {
            AbyssRankService.AddAp(player, apReward);
            await _conn.SendAsync(new SM_ABYSS_RANK(player.AbyssPoints, player.AbyssRank), ct);
            await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, ct);
        }

        // Award title (auto-equip; mirrors Java TitleList.addTitle(id, true, 0))
        int titleReward = template.Rewards?.Title ?? -1;
        if (titleReward >= 0)
        {
            player.TitleId = titleReward;
            await _playerDao.UpdateTitleAsync(player.ObjectId, titleReward, ct);
            await _conn.SendAsync(SM_TITLE_INFO.ActiveTitle(titleReward), ct);
            int titleWorldId = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
            {
                if (c == _conn || c.ActivePlayer?.Position.WorldId != titleWorldId) continue;
                try { await c.SendAsync(SM_TITLE_INFO.BroadcastTitle(player.ObjectId, titleReward), ct); } catch { }
            }
        }

        // Mark complete
        entry.Status        = QuestStatus.COMPLETE;
        entry.CompleteCount = Math.Min(entry.CompleteCount + 1, 127);
        await _questDao.UpsertAsync(player.ObjectId, entry, ct);

        await _conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.StepUpdate, (byte)QuestStatus.COMPLETE, entry.Step), ct);
        await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        await _conn.SendAsync(new SM_QUEST_COMPLETED_LIST(player.Quests.Completed), ct);

        _log.LogInformation("Player {Name} completed quest {QuestId} ({QuestName}), exp={Exp}",
            player.Name, _questId, template.Name, expReward);
    }
}
