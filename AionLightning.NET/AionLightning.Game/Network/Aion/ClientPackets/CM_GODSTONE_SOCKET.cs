using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client sockets a godstone into a weapon. Opcode 0x139.
/// Mirrors Java ItemSocketService.socketGodstone:
///   validates weapon/stone, checks kinah (100 000 per Java PricesService base),
///   consumes stone, sets weapon.GodStoneItemId, persists, broadcasts appearance.
/// </summary>
public sealed class CM_GODSTONE_SOCKET : AionClientPacket
{
    private const long SocketCost  = 100_000;  // Java PricesService base = 100000
    private const int  KinahItemId = 182400001;

    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _npcObjectId;
    private int _weaponUniqueId;
    private int _stoneUniqueId;

    public CM_GODSTONE_SOCKET(GsClientConnection conn, IItemDao itemDao,
        IDataManager dataManager, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _npcObjectId    = r.ReadD();
        _weaponUniqueId = r.ReadD();
        _stoneUniqueId  = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Weapon can be in inventory or equipped
        var weapon = player.Inventory.Get(_weaponUniqueId)
                  ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _weaponUniqueId);
        if (weapon is null) return;

        var weaponTemplate = _dataManager.Items.GetTemplate(weapon.ItemId);
        if (weaponTemplate is null || !weaponTemplate.CanSocketGodstone)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.GodstoneInvalid(), ct);
            return;
        }

        if (weapon.GodStoneItemId != 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.GodstoneAlreadySlotted(), ct);
            return;
        }

        var stone = player.Inventory.Get(_stoneUniqueId);
        if (stone is null) return;

        var stoneTemplate = _dataManager.Items.GetTemplate(stone.ItemId);
        if (stoneTemplate?.Godstone is null)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.GodstoneInvalid(), ct);
            return;
        }

        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if (kinahItem is null || kinahItem.Count < SocketCost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.GodstoneNoKinah(), ct);
            return;
        }

        // Deduct kinah
        kinahItem.Count -= SocketCost;

        // Consume 1 godstone
        stone.Count--;
        if (stone.Count <= 0)
        {
            player.Inventory.Remove(stone.UniqueId);
            await _itemDao.DeleteAsync(stone.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM((int)stone.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([stone]), ct);
        }

        // Apply godstone
        weapon.GodStoneItemId = stone.ItemId;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify player of updated weapon and kinah
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([weapon, kinahItem]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.GodstoneApplied(), ct);

        // Broadcast updated appearance if weapon is equipped
        if (weapon.IsEquipped)
        {
            int worldId = player.Position.WorldId;
            var appearance = new SM_UPDATE_PLAYER_APPEARANCE(player.ObjectId, player.Inventory.All);
            try { await _conn.SendAsync(appearance, ct); } catch { }
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == worldId)
                    try { await other.SendAsync(appearance, ct); } catch { }
        }
    }
}
