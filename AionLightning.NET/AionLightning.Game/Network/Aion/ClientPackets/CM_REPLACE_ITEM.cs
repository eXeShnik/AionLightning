using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Swaps two items between any storage combination. Opcode 0x170.</summary>
public sealed class CM_REPLACE_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;

    private byte _sourceStorageType;
    private int  _sourceItemObjId;
    private byte _replaceStorageType;
    private int  _replaceItemObjId;

    public CM_REPLACE_ITEM(GsClientConnection conn, IItemDao itemDao)
    {
        _conn    = conn;
        _itemDao = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _sourceStorageType  = (byte)r.ReadC();
        _sourceItemObjId    = r.ReadD();
        _replaceStorageType = (byte)r.ReadC();
        _replaceItemObjId   = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var sourceStorage  = _sourceStorageType  == 0 ? player.Inventory : player.Warehouse;
        var replaceStorage = _replaceStorageType == 0 ? player.Inventory : player.Warehouse;

        var sourceItem  = sourceStorage.Get(_sourceItemObjId);
        var replaceItem = replaceStorage.Get(_replaceItemObjId);
        if (sourceItem is null || replaceItem is null) return;

        // Swap slots
        (sourceItem.Slot, replaceItem.Slot) = (replaceItem.Slot, sourceItem.Slot);

        if (_sourceStorageType == _replaceStorageType)
        {
            // Same-storage swap: save + notify
            if (_sourceStorageType == 0)
            {
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([sourceItem, replaceItem]), ct);
            }
            else
            {
                await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);
                await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
            }
        }
        else
        {
            // Cross-storage swap: move each item to the other container
            sourceStorage.Remove(sourceItem.UniqueId);
            replaceStorage.Remove(replaceItem.UniqueId);

            sourceItem.StorageType  = _replaceStorageType;
            replaceItem.StorageType = _sourceStorageType;

            replaceStorage.Add(sourceItem);
            sourceStorage.Add(replaceItem);

            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);

            // Send delete for the item that left inventory, add for the one that entered
            if (_sourceStorageType == 0)
            {
                // source left inventory, replace entered inventory
                await _conn.SendAsync(new SM_DELETE_ITEM(sourceItem.UniqueId), ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([replaceItem]), ct);
            }
            else
            {
                // replace left inventory, source entered inventory
                await _conn.SendAsync(new SM_DELETE_ITEM(replaceItem.UniqueId), ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([sourceItem]), ct);
            }
            await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
        }
    }
}
