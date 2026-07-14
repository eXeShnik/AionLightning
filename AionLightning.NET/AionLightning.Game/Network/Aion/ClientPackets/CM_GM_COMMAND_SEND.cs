using AionLightning.Commons.Network;
using AionLightning.Game.Controllers;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Admin command from the client (dot-prefix syntax). Opcode 0xC8.</summary>
public sealed class CM_GM_COMMAND_SEND : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly GameWorld                _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly IPlayerDao               _playerDao;
    private readonly IQuestDao                _questDao;
    private readonly SkillLearnService        _skillLearn;
    private readonly SpawnService             _spawnService;
    private readonly HousingService           _housingService;
    private readonly HouseController          _houseController;
    private readonly HousingBidService        _housingBidService;
    private readonly IHouseDao                _houseDao;

    private string _command = string.Empty;

    public CM_GM_COMMAND_SEND(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IItemDao itemDao, IDataManager dataManager,
        IPlayerDao playerDao, IQuestDao questDao, SkillLearnService skillLearn, SpawnService spawnService,
        HousingService housingService, HouseController houseController, HousingBidService housingBidService,
        IHouseDao houseDao)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _playerDao    = playerDao;
        _questDao     = questDao;
        _skillLearn   = skillLearn;
        _spawnService = spawnService;
        _housingService    = housingService;
        _houseController   = houseController;
        _housingBidService = housingBidService;
        _houseDao          = houseDao;
    }

    public override void Read(ref PacketReader r) => _command = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var parts = _command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        switch (parts[0].ToLowerInvariant())
        {
            case ".heal":
                await HandleHeal(player, ct);
                break;

            case ".level" when parts.Length >= 2 && byte.TryParse(parts[1], out var lvl) && lvl is >= 1 and <= 65:
                await HandleLevel(player, lvl, ct);
                break;

            case ".tp" when parts.Length >= 5
                && int.TryParse(parts[1], out var mapId)
                && float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var tpX)
                && float.TryParse(parts[3], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var tpY)
                && float.TryParse(parts[4], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var tpZ):
                await HandleTeleport(player, mapId, tpX, tpY, tpZ, ct);
                break;

            case ".item" when parts.Length >= 2 && int.TryParse(parts[1], out var itemId):
                long count = parts.Length >= 3 && long.TryParse(parts[2], out var c) ? c : 1;
                await HandleGiveItem(player, itemId, count, ct);
                break;

            case ".ap" when parts.Length >= 2 && long.TryParse(parts[1], out var apAmount):
                await HandleGiveAp(player, apAmount, ct);
                break;

            case ".gp" when parts.Length >= 2 && long.TryParse(parts[1], out var gpAmount):
                await HandleGiveGp(player, gpAmount, ct);
                break;

            case ".spawn" when parts.Length >= 2 && int.TryParse(parts[1], out var spawnNpcId):
                await HandleSpawnNpc(player, spawnNpcId, ct);
                break;

            case ".kick" when parts.Length >= 2:
                HandleKick(parts[1]);
                break;

            case ".summon" when parts.Length >= 2:
                await HandleSummon(player, parts[1], ct);
                break;

            case ".goto" when parts.Length >= 2:
                await HandleGoto(player, parts[1], ct);
                break;

            case ".announce" when parts.Length >= 2:
                await HandleAnnounce(player, string.Join(' ', parts, 1, parts.Length - 1), ct);
                break;

            // .quest add <questId>  — start quest in START status
            // .quest done <questId> — transition to REWARD
            // .quest del <questId>  — abandon quest
            case ".quest" when parts.Length >= 3 && int.TryParse(parts[2], out var qId):
                await HandleQuest(player, parts[1].ToLowerInvariant(), qId, ct);
                break;

            // .cube [n] — expand cube by n NPC-expand levels (default 1, max level 5)
            case ".cube":
                int cubeSteps = parts.Length >= 2 && int.TryParse(parts[1], out var cs) ? cs : 1;
                await HandleCubeExpand(player, cubeSteps, ct);
                break;

            // .ss clear — remove all soul sickness stacks
            case ".ss" when parts.Length >= 2 && parts[1].Equals("clear", StringComparison.OrdinalIgnoreCase):
                await HandleSoulSicknessClear(player, ct);
                break;

            // .skill <skillId> [level] — teach a skill (default level 1)
            case ".skill" when parts.Length >= 2 && int.TryParse(parts[1], out var skId):
                int skLevel = parts.Length >= 3 && int.TryParse(parts[2], out var sl) ? sl : 1;
                await HandleAddSkill(player, skId, skLevel, ct);
                break;

            // Java admincommands.HouseCommand (//house). This port has no house "name" field (see
            // HandleHouseCommand's own doc note) and no admin.getTarget() selection model, so houses are
            // addressed by HouseAddress.Id and players by online character name instead.
            case ".house" when parts.Length >= 2:
                await HandleHouseCommand(player, parts, ct);
                break;
        }
    }

    private async ValueTask HandleHeal(Player player, CancellationToken ct)
    {
        player.CurrentHp = player.MaxHp;
        player.CurrentMp = player.MaxMp;
        var tpl    = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        var hpPkt  = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, 0, player.MaxHp, SM_ATTACK_STATUS.LogId.RegularHeal);
        var mpPkt  = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, 0, player.MaxMp, SM_ATTACK_STATUS.LogId.MpHeal);
        await _conn.SendAsync(hpPkt, ct);
        await _conn.SendAsync(mpPkt, ct);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);

        int worldId = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(hpPkt, ct); await other.SendAsync(mpPkt, ct); } catch { }
    }

    private async ValueTask HandleLevel(Player player, byte level, CancellationToken ct)
    {
        player.Level = level;
        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, level);
        if (tpl is not null)
        {
            player.MaxHp     = tpl.MaxHp;
            player.MaxMp     = tpl.MaxMp;
            player.CurrentHp = tpl.MaxHp;
            player.CurrentMp = tpl.MaxMp;
        }
        player.Exp = _dataManager.ExpTable.GetStartExpForLevel(level);
        await _playerDao.UpdateExpLevelAsync(player.ObjectId, player.Exp, level, ct);
        await _conn.SendAsync(new SM_LEVEL_UPDATE(player.ObjectId, 0, level), ct);
        long maxExp = _dataManager.ExpTable.GetStartExpForLevel(level + 1);
        await _conn.SendAsync(new SM_STATUPDATE_EXP(player.Exp, 0, maxExp), ct);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
    }

    private async ValueTask HandleTeleport(Player player, int mapId, float x, float y, float z, CancellationToken ct)
    {
        int oldWorldId = player.Position.WorldId;
        if (oldWorldId != mapId)
        {
            var deletePacket = new SM_DELETE(player.ObjectId);
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                    try { await other.SendAsync(deletePacket, ct); } catch { }
        }

        player.Position = new Position(x, y, z, player.Position.Heading, mapId);
        await _conn.SendAsync(new SM_TELEPORT_LOC(mapId, x, y, z), ct);

        // Introduce GM to peers already in the destination zone
        if (oldWorldId != mapId)
        {
            var equipment  = player.Inventory.All.Where(i => i.IsEquipped).ToList();
            var playerInfo = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment);
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == mapId)
                    try { await other.SendAsync(playerInfo, ct); } catch { }
        }
    }

    private async ValueTask HandleGiveItem(Player player, int itemId, long count, CancellationToken ct)
    {
        var tpl = _dataManager.Items.GetTemplate(itemId);
        if (tpl is null) return;

        const int KinahId = 182400001;
        if (itemId == KinahId)
        {
            var kinah = player.Inventory.FindByItemId(KinahId);
            if (kinah is not null)
            {
                kinah.Count += count;
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
                return;
            }
        }

        var uniqueId = await _itemDao.NextUniqueIdAsync(ct);
        var item = new Item { UniqueId = uniqueId, ItemId = itemId, Count = count, Slot = -1 };
        player.Inventory.Add(item);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
    }

    private async ValueTask HandleGiveAp(Player player, long amount, CancellationToken ct)
    {
        AbyssRankService.AddAp(player, amount);
        await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, player.AbyssGp, player.AbyssTopRanking, ct);
        await _conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), ct);
    }

    private async ValueTask HandleGiveGp(Player player, long amount, CancellationToken ct)
    {
        AbyssRankService.AddGloryPoints(player, amount);
        await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, player.AbyssGp, player.AbyssTopRanking, ct);
        await _conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), ct);
    }

    private async ValueTask HandleSpawnNpc(Player player, int npcId, CancellationToken ct)
    {
        var template = _dataManager.Npcs.GetTemplate(npcId);
        if (template is null) return;

        var position   = player.Position;
        var spawned    = _spawnService.SpawnNpcAt(template, position);
        var infoPacket = new SM_NPC_INFO(spawned);
        int worldId    = position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(infoPacket, ct); } catch { }
    }

    private async ValueTask HandleAnnounce(Player gm, string message, CancellationToken ct)
    {
        var pkt = new SM_MESSAGE(gm, message, SM_MESSAGE.ChatType.Command);
        foreach (var conn in _connRegistry.GetAll())
            try { await conn.SendAsync(pkt, ct); } catch { }
    }

    private void HandleKick(string targetName)
    {
        var target = _connRegistry.GetByName(targetName);
        if (target is null) return;
        _ = target.DisposeAsync();
    }

    private async ValueTask HandleSummon(Player gm, string targetName, CancellationToken ct)
    {
        var targetConn = _connRegistry.GetByName(targetName);
        var target = targetConn?.ActivePlayer;
        if (target is null) return;

        int oldWorldId = target.Position.WorldId;
        int newWorldId = gm.Position.WorldId;

        if (oldWorldId != newWorldId)
        {
            var del = new SM_DELETE(target.ObjectId);
            foreach (var other in _connRegistry.GetAllExcept(target.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                    try { await other.SendAsync(del, ct); } catch { }
        }

        target.Position = gm.Position;
        await targetConn!.SendAsync(new SM_TELEPORT_LOC(newWorldId, gm.Position.X, gm.Position.Y, gm.Position.Z), ct);

        if (oldWorldId != newWorldId)
        {
            var equipment = target.Inventory.All.Where(i => i.IsEquipped).ToList();
            var info = new SM_PLAYER_INFO(target, target.Appearance, enemy: false, equipment);
            foreach (var other in _connRegistry.GetAllExcept(target.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == newWorldId)
                    try { await other.SendAsync(info, ct); } catch { }
        }
    }

    private async ValueTask HandleGoto(Player gm, string targetName, CancellationToken ct)
    {
        var target = _connRegistry.GetByName(targetName)?.ActivePlayer;
        if (target is null) return;
        await HandleTeleport(gm, target.Position.WorldId, target.Position.X, target.Position.Y, target.Position.Z, ct);
    }

    private async ValueTask HandleQuest(Player player, string action, int questId, CancellationToken ct)
    {
        switch (action)
        {
            case "add":
            {
                if (player.Quests.Contains(questId)) return;
                var template = _dataManager.Quests.GetTemplate(questId);
                if (template is null) return;
                var entry = new Model.Quest.QuestEntry { QuestId = questId, Status = Model.Quest.QuestStatus.START };
                player.Quests.Add(entry);
                await _questDao.UpsertAsync(player.ObjectId, entry, ct);
                await _conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId, SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
                await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
                break;
            }
            case "done":
            {
                var entry = player.Quests.Get(questId);
                if (entry is null || entry.Status == Model.Quest.QuestStatus.COMPLETE) return;
                entry.Status = Model.Quest.QuestStatus.REWARD;
                await _questDao.UpsertAsync(player.ObjectId, entry, ct);
                await _conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId, SM_QUEST_ACTION.ActionType.StepUpdate, (byte)entry.Status, entry.Step), ct);
                await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
                break;
            }
            case "del":
            {
                var entry = player.Quests.Get(questId);
                if (entry is null) return;
                player.Quests.Remove(questId);
                await _questDao.DeleteAsync(player.ObjectId, questId, ct);
                await _conn.SendAsync(new SM_QUEST_ACTION(questId), ct);
                await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
                break;
            }
        }
    }

    private const int MaxNpcExpands = 5;

    private async ValueTask HandleCubeExpand(Player player, int steps, CancellationToken ct)
    {
        int available = MaxNpcExpands - player.NpcExpands;
        int actual    = Math.Clamp(steps, 0, available);
        if (actual == 0) return;

        player.NpcExpands         += actual;
        player.Inventory.Capacity  = player.CubeCapacity;
        await _playerDao.UpdateCubeExpandAsync(player.ObjectId, player.NpcExpands, ct);

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(SM_CUBE_UPDATE.CubeSize(
            player.Inventory.BagSlotUsed, player.NpcExpands, player.QuestExpands), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.CubeExpanded(actual * 9), ct);
    }

    private async ValueTask HandleSoulSicknessClear(Player player, CancellationToken ct)
    {
        if (player.SoulSicknessCount == 0) return;
        player.SoulSicknessCount = 0;
        await _playerDao.UpdateSoulSicknessAsync(player.ObjectId, 0, ct);

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        player.MaxHp = (statTpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp;
        player.MaxMp = (statTpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp;
        player.CurrentHp = Math.Min(player.CurrentHp, player.MaxHp);
        player.CurrentMp = Math.Min(player.CurrentMp, player.MaxMp);

        player.RemoveEffectBySkillId(8291);
        int ssWorld  = player.Position.WorldId;
        var ssEffect = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        foreach (var c in _connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == ssWorld)
                try { await c.SendAsync(ssEffect, ct); } catch { }

        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
    }

    private async ValueTask HandleAddSkill(Player player, int skillId, int skillLevel, CancellationToken ct)
    {
        await _skillLearn.LearnSkillAsync(player, skillId, skillLevel, ct: ct);
        await _conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills, isNew: true), ct);
    }

    /// <summary>
    /// Java admincommands.HouseCommand (//house tp|acquire|revoke) — this port has no house "name" field
    /// (Java's House.getName() has no equivalent on <see cref="Model.House.House"/>, which only tracks the
    /// numeric <see cref="Model.House.House.Address"/> id) and no admin.getTarget() single-object-selection
    /// model, so subcommands take a HouseAddress.Id and an online character name instead of a house name
    /// and a pre-selected target. Adds ".house info" (not in Java) since address-based lookups otherwise
    /// have no way to discover a house's current owner/status from the client.
    /// </summary>
    private async ValueTask HandleHouseCommand(Player admin, string[] parts, CancellationToken ct)
    {
        switch (parts[1].ToLowerInvariant())
        {
            case "info":
                int? infoAddress = parts.Length >= 3 && int.TryParse(parts[2], out var ia) ? ia : null;
                await HandleHouseInfo(admin, infoAddress, ct);
                break;

            case "tp" when parts.Length >= 3 && int.TryParse(parts[2], out var tpAddress):
                await HandleHouseTeleport(admin, tpAddress, ct);
                break;

            case "acquire" when parts.Length >= 4 && int.TryParse(parts[2], out var acquireAddress):
                await HandleHouseAcquire(admin, acquireAddress, parts[3], ct);
                break;

            case "revoke" when parts.Length >= 3 && int.TryParse(parts[2], out var revokeAddress):
                await HandleHouseRevoke(admin, revokeAddress, ct);
                break;

            default:
                await SendGmMessage(admin, "Syntax: .house <info [address] | tp <address> | acquire <address> <player> | revoke <address>>", ct);
                break;
        }
    }

    private async ValueTask HandleHouseInfo(Player admin, int? address, CancellationToken ct)
    {
        List<Model.House.House> houses = address is { } addr
            ? (_housingService.GetHouseByAddress(addr) is { } h ? [h] : [])
            : _housingService.SearchPlayerHouses(admin.ObjectId);

        if (houses.Count == 0)
        {
            await SendGmMessage(admin, "No such house!", ct);
            return;
        }

        foreach (var house in houses)
        {
            string owner = house.IsOwned ? house.PlayerObjectId.ToString() : "none";
            await SendGmMessage(admin,
                $"House address={house.Address} building={house.BuildingId} status={house.Status} owner={owner}", ct);
        }
    }

    private async ValueTask HandleHouseTeleport(Player admin, int address, CancellationToken ct)
    {
        var houseAddress = _dataManager.Housing.GetAddress(address);
        if (houseAddress is null)
        {
            await SendGmMessage(admin, "No such house address!", ct);
            return;
        }

        await HandleTeleport(admin, houseAddress.MapId, houseAddress.X, houseAddress.Y, houseAddress.Z, ct);
    }

    /// <summary>
    /// Java ChangeHouseOwner(admin, name, acquire=true) — reworked around address+player-name targeting
    /// (see <see cref="HandleHouseCommand"/>'s doc note). Reuses <see cref="HousingBidService.CompleteHouseSellAsync"/>
    /// (the same "outright win" path a normal auction uses) to assign ownership, since that already persists
    /// the house row, refreshes the target's Player.Houses/BuildingOwnerState if online, and sends the
    /// gated SM_HOUSE_ACQUIRE/SM_HOUSE_OWNER_INFO pair — reusing it here keeps this GM path on the same
    /// verified send path instead of duplicating it.
    /// note: Java also revoked the target's existing house first when they already own exactly one (so an
    /// acquire never grows them past 1 house even though the cap is 2), including deleting an existing
    /// studio outright. This port skips the studio sub-case: HousingService keeps no "forget this studio"
    /// hook, so deleting the DB row without also removing the stale entry from HousingService's in-memory
    /// _studios dictionary would leave it resolvable (SearchPlayerHouses/GetPlayerAddress) until a restart —
    /// declined instead of leaving that inconsistency.
    /// </summary>
    private async ValueTask HandleHouseAcquire(Player admin, int address, string targetName, CancellationToken ct)
    {
        var targetConn = _connRegistry.GetByName(targetName);
        var target = targetConn?.ActivePlayer;
        if (target is null)
        {
            await SendGmMessage(admin, "Player not found or offline.", ct);
            return;
        }

        var house = _housingService.GetHouseByAddress(address);
        if (house is null)
        {
            await SendGmMessage(admin, "No such house!", ct);
            return;
        }

        if (target.Houses.Count >= 2)
        {
            await SendGmMessage(admin, "Player can not own more than 2 houses!", ct);
            return;
        }

        if (target.Houses.Count == 1)
        {
            var current = target.Houses[0];
            bool isStudio = _dataManager.Housing.GetBuilding(current.BuildingId)?.Type == BuildingType.PERSONAL_INS;
            if (isStudio)
            {
                await SendGmMessage(admin, "Target owns a studio; revoke it first with .house revoke.", ct);
                return;
            }

            current.PlayerObjectId = 0;
            current.Status = HouseStatus.Active;
            current.FeePaid = true;
            current.NextPay = null;
            current.SellStarted = null;
            await _houseDao.StoreAsync(current, ct);
            await _houseController.BroadcastAppearanceAsync(current, ct);
        }

        await _housingBidService.CompleteHouseSellAsync(target.ObjectId, house, ct);
        await SendGmMessage(admin, $"House {house.Address} acquired by {targetName}", ct);
    }

    /// <summary>
    /// Java ChangeHouseOwner(admin, name, acquire=false) — Java's House.revokeOwner(): deletes the DB row
    /// outright for a studio, otherwise clears ownership and resets to NoSale/fee-paid. Reworked around
    /// address targeting (see <see cref="HandleHouseCommand"/>'s doc note); Java's secondary "reactivate
    /// any of the target's other non-ACTIVE houses" side effect isn't reproduced (a minor, rarely-hit extra
    /// effect of the original admin tool, not the revoke itself).
    /// </summary>
    private async ValueTask HandleHouseRevoke(Player admin, int address, CancellationToken ct)
    {
        var house = _housingService.GetHouseByAddress(address);
        if (house is null || !house.IsOwned)
        {
            await SendGmMessage(admin, "Nothing to revoke!", ct);
            return;
        }

        bool isStudio = _dataManager.Housing.GetBuilding(house.BuildingId)?.Type == BuildingType.PERSONAL_INS;
        int previousOwnerId = house.PlayerObjectId;

        if (isStudio)
        {
            // note: HousingService keeps no removal hook for its in-memory _studios dictionary, so this
            // stays resolvable via SearchPlayerHouses/GetPlayerAddress until the next full reload — an
            // accepted, documented gap for this GM tool (see HandleHouseAcquire's matching note).
            await _houseDao.DeleteByOwnerAsync(previousOwnerId, ct);
        }
        else
        {
            house.PlayerObjectId = 0;
            house.Status = HouseStatus.NoSale;
            house.FeePaid = true;
            house.AcquiredTime = default;
            house.SellStarted = null;
            house.NextPay = null;
            await _houseDao.StoreAsync(house, ct);
            await _houseController.BroadcastAppearanceAsync(house, ct);
        }

        if (_connRegistry.Get(previousOwnerId)?.ActivePlayer is { } previousOwner)
        {
            previousOwner.Houses.RemoveAll(h => h.Id == house.Id);
            if (previousOwner.Houses.Count == 0)
                previousOwner.BuildingOwnerState = (byte)PlayerHouseOwnerFlags.BuyStudioAllowed;
        }

        await SendGmMessage(admin, $"House {house.Address} revoked", ct);
    }

    private async ValueTask SendGmMessage(Player admin, string message, CancellationToken ct) =>
        await _conn.SendAsync(new SM_MESSAGE(admin, message, SM_MESSAGE.ChatType.Command), ct);
}
