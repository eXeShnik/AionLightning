using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Player drags an item between bag slots or between inventory / personal / account / legion warehouse.
/// Opcode 0x17E.
/// Storage types: 0 = inventory, 1 = personal warehouse, 2 = account warehouse, 3 = legion warehouse.
/// </summary>
public sealed class CM_MOVE_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;
    private readonly ILegionDao _legionDao;

    private int   _itemObjectId;
    private byte  _source;
    private byte  _destination;
    private short _slot;

    public CM_MOVE_ITEM(GsClientConnection conn, IItemDao itemDao, ILegionDao legionDao)
    {
        _conn      = conn;
        _itemDao   = itemDao;
        _legionDao = legionDao;
    }

    public override void Read(ref PacketReader r)
    {
        _itemObjectId = r.ReadD();
        _source       = (byte)r.ReadC();
        _destination  = (byte)r.ReadC();
        _slot         = (short)r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_source, _destination)
        {
            case (0, 0): // inventory → inventory reorder (swap if target occupied)
            {
                var item = player.Inventory.Get(_itemObjectId);
                if (item is null || item.IsEquipped) return;

                // If another bag item already sits in the target slot, swap them
                var displaced = player.Inventory.All
                    .FirstOrDefault(i => !i.IsEquipped && i.Slot == _slot && i.UniqueId != item.UniqueId);
                if (displaced is not null)
                    displaced.Slot = item.Slot;

                item.Slot = _slot;
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

                var changed = displaced is not null
                    ? new[] { item, displaced }
                    : new[] { item };
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(changed), ct);
                break;
            }

            case (0, 1): // inventory → personal warehouse
            {
                var item = player.Inventory.Get(_itemObjectId);
                if (item is null || item.IsEquipped) return;
                player.Inventory.Remove(item.UniqueId);
                item.StorageType = 1;
                item.Slot        = _slot;
                player.Warehouse.Add(item);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);
                // Remove from inventory UI, then send updated warehouse
                await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
                await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
                break;
            }

            case (1, 0): // personal warehouse → inventory
            {
                var item = player.Warehouse.Get(_itemObjectId);
                if (item is null) return;
                player.Warehouse.Remove(item.UniqueId);
                item.StorageType = 0;
                item.Slot        = _slot;
                player.Inventory.Add(item);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
                await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
                break;
            }

            case (1, 1): // warehouse → warehouse reorder (swap if target occupied)
            {
                var item = player.Warehouse.Get(_itemObjectId);
                if (item is null) return;

                var displaced = player.Warehouse.All
                    .FirstOrDefault(i => i.Slot == _slot && i.UniqueId != item.UniqueId);
                if (displaced is not null)
                    displaced.Slot = item.Slot;

                item.Slot = _slot;
                await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);
                await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
                break;
            }

            case (0, 2): // inventory → account warehouse
            {
                var item = player.Inventory.Get(_itemObjectId);
                if (item is null || item.IsEquipped) return;
                player.Inventory.Remove(item.UniqueId);
                item.StorageType = 2;
                item.Slot        = _slot;
                player.AccountWarehouse.Add(item);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _itemDao.SaveAccountWarehouseAsync(_conn.AccountId, player.AccountWarehouse.All, ct);
                await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
                await _conn.SendAsync(new SM_ACCOUNT_WAREHOUSE_INFO(player.AccountWarehouse.All), ct);
                break;
            }

            case (2, 0): // account warehouse → inventory
            {
                var item = player.AccountWarehouse.Get(_itemObjectId);
                if (item is null) return;
                player.AccountWarehouse.Remove(item.UniqueId);
                item.StorageType = 0;
                item.Slot        = _slot;
                player.Inventory.Add(item);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _itemDao.SaveAccountWarehouseAsync(_conn.AccountId, player.AccountWarehouse.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
                await _conn.SendAsync(new SM_ACCOUNT_WAREHOUSE_INFO(player.AccountWarehouse.All), ct);
                break;
            }

            case (2, 2): // account warehouse → account warehouse reorder
            {
                var item = player.AccountWarehouse.Get(_itemObjectId);
                if (item is null) return;

                var displaced = player.AccountWarehouse.All
                    .FirstOrDefault(i => i.Slot == _slot && i.UniqueId != item.UniqueId);
                if (displaced is not null)
                    displaced.Slot = item.Slot;

                item.Slot = _slot;
                await _itemDao.SaveAccountWarehouseAsync(_conn.AccountId, player.AccountWarehouse.All, ct);
                await _conn.SendAsync(new SM_ACCOUNT_WAREHOUSE_INFO(player.AccountWarehouse.All), ct);
                break;
            }

            case (0, 3): // inventory → legion warehouse
            {
                var legion = player.Legion;
                if (legion is null) return;
                var item = player.Inventory.Get(_itemObjectId);
                if (item is null || item.IsEquipped) return;
                player.Inventory.Remove(item.UniqueId);
                item.StorageType = 3;
                item.Slot        = _slot;
                legion.WarehouseItems.Add(item);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _legionDao.SaveWarehouseItemsAsync(legion.LegionId, legion.WarehouseItems.All, ct);
                await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
                await _conn.SendAsync(new SM_LEGION_WAREHOUSE_INFO(legion.WarehouseItems.All), ct);
                break;
            }

            case (3, 0): // legion warehouse → inventory
            {
                var legion = player.Legion;
                if (legion is null) return;
                var item = legion.WarehouseItems.Get(_itemObjectId);
                if (item is null) return;
                legion.WarehouseItems.Remove(item.UniqueId);
                item.StorageType = 0;
                item.Slot        = _slot;
                player.Inventory.Add(item);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _legionDao.SaveWarehouseItemsAsync(legion.LegionId, legion.WarehouseItems.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
                await _conn.SendAsync(new SM_LEGION_WAREHOUSE_INFO(legion.WarehouseItems.All), ct);
                break;
            }

            case (3, 3): // legion warehouse → legion warehouse reorder
            {
                var legion = player.Legion;
                if (legion is null) return;
                var item = legion.WarehouseItems.Get(_itemObjectId);
                if (item is null) return;

                var displaced = legion.WarehouseItems.All
                    .FirstOrDefault(i => i.Slot == _slot && i.UniqueId != item.UniqueId);
                if (displaced is not null)
                    displaced.Slot = item.Slot;

                item.Slot = _slot;
                await _legionDao.SaveWarehouseItemsAsync(legion.LegionId, legion.WarehouseItems.All, ct);
                await _conn.SendAsync(new SM_LEGION_WAREHOUSE_INFO(legion.WarehouseItems.All), ct);
                break;
            }
        }
    }
}
