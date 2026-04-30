using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
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
    private readonly SpawnService             _spawnService;

    private string _command = string.Empty;

    public CM_GM_COMMAND_SEND(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IItemDao itemDao, IDataManager dataManager,
        IPlayerDao playerDao, SpawnService spawnService)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _playerDao    = playerDao;
        _spawnService = spawnService;
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

            case ".spawn" when parts.Length >= 2 && int.TryParse(parts[1], out var spawnNpcId):
                await HandleSpawnNpc(player, spawnNpcId, ct);
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
        await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, ct);
        await _conn.SendAsync(new SM_ABYSS_RANK(player.AbyssPoints, player.AbyssRank), ct);
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
}
