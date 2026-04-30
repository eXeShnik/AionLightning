using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deposits/withdraws kinah from legion warehouse. Opcode 0x2EE.</summary>
public sealed class CM_LEGION_WH_KINAH : AionClientPacket
{
    private const int KinahItemId = 182400001;

    private readonly GsClientConnection _conn;
    private readonly ILegionDao         _legionDao;
    private readonly IItemDao           _itemDao;

    private long _amount;
    private int  _operation;

    public CM_LEGION_WH_KINAH(GsClientConnection conn, ILegionDao legionDao, IItemDao itemDao)
    {
        _conn      = conn;
        _legionDao = legionDao;
        _itemDao   = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _amount    = r.ReadQ();
        _operation = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var legion = player.Legion;
        if (legion is null) return;

        if (_amount <= 0) return;

        switch (_operation)
        {
            case 0: // withdraw from warehouse to player inventory
            {
                if (legion.WarehouseKinah < _amount) return;

                legion.WarehouseKinah -= _amount;
                await _legionDao.UpdateWarehouseKinahAsync(legion.LegionId, legion.WarehouseKinah, ct);

                var kinahItem = player.Inventory.FindByItemId(KinahItemId, includeEquipped: true);
                if (kinahItem is null)
                {
                    long uid = await _itemDao.NextUniqueIdAsync(ct);
                    kinahItem = new Item { UniqueId = uid, ItemId = KinahItemId, Count = _amount, Slot = -1 };
                    player.Inventory.Add(kinahItem);
                }
                else
                {
                    kinahItem.Count += _amount;
                }
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
                break;
            }
            case 1: // deposit from player inventory to warehouse
            {
                var kinahItem = player.Inventory.FindByItemId(KinahItemId, includeEquipped: true);
                if (kinahItem is null || kinahItem.Count < _amount) return;

                kinahItem.Count -= _amount;
                if (kinahItem.Count == 0)
                {
                    player.Inventory.Remove(kinahItem.UniqueId);
                    await _itemDao.DeleteAsync(kinahItem.UniqueId, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM(kinahItem.UniqueId), ct);
                }
                else
                {
                    await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                    await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
                }

                legion.WarehouseKinah += _amount;
                await _legionDao.UpdateWarehouseKinahAsync(legion.LegionId, legion.WarehouseKinah, ct);
                break;
            }
        }
    }
}
