using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client submits an appearance or rename request. Opcode 0x167.
/// type=0: player rename (consumes a rename item, updates name in DB, re-broadcasts).
/// type=1: legion rename — stub (no-op; requires LegionService integration).
/// type=2: cosmetic item — stub (no-op; requires CosmeticItemAction).
/// </summary>
public sealed class CM_APPEARANCE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IPlayerDao               _playerDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IItemDao                 _itemDao;

    private byte   _type;
    private int    _itemObjId;
    private string _name = string.Empty;

    public CM_APPEARANCE(GsClientConnection conn, IPlayerDao playerDao,
        PlayerConnectionRegistry connRegistry, IItemDao itemDao)
    {
        _conn         = conn;
        _playerDao    = playerDao;
        _connRegistry = connRegistry;
        _itemDao      = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _type     = (byte)r.ReadC();
        r.ReadC();
        r.ReadH();
        _itemObjId = r.ReadD();
        if (_type is 0 or 1)
            _name = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (_type == 0)
            await HandlePlayerRenameAsync(player, ct);
        // type 1 (legion rename) and type 2 (cosmetic) remain stubs
    }

    private async ValueTask HandlePlayerRenameAsync(Player player, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_name) || _name.Length < 2 || _name.Length > 16) return;

        // Must have the rename item in inventory
        var renameItem = player.Inventory.Get(_itemObjId);
        if (renameItem is null) return;

        // Name must be unique
        if (await _playerDao.ExistsByNameAsync(_name, ct)) return;

        // Consume the rename item
        renameItem.Count--;
        if (renameItem.Count <= 0)
        {
            player.Inventory.Remove(renameItem.UniqueId);
            await _itemDao.DeleteAsync(renameItem.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM((int)renameItem.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([renameItem]), ct);
        }

        // Update name
        player.Name = _name;
        await _playerDao.UpdateNameAsync(player.ObjectId, _name, ct);

        // Re-broadcast player info to zone peers
        int worldId = player.Position.WorldId;
        var equipment = player.Inventory.All.Where(i => i.IsEquipped).ToList();
        var playerInfo = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment);
        await _conn.SendAsync(playerInfo, ct);
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(playerInfo, ct); } catch { }
    }
}
