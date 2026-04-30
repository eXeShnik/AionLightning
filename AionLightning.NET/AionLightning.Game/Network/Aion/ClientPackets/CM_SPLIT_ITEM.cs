using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Player splits a stack into two, or merges it into an existing same-item stack. Opcode 0x17F.
/// Storage types: 0 = inventory, 1 = personal warehouse.
/// </summary>
public sealed class CM_SPLIT_ITEM : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;

    private int  _sourceItemObjId;
    private long _splitAmount;
    private int  _destinationItemObjId;
    private byte _sourceStorageType;
    private byte _destinationStorageType;
    private short _slotNum;

    public CM_SPLIT_ITEM(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        _sourceItemObjId      = r.ReadD();
        _splitAmount          = r.ReadD();
        r.ReadB(4);                         // padding
        _sourceStorageType      = (byte)r.ReadC();
        _destinationItemObjId = r.ReadD();
        _destinationStorageType = (byte)r.ReadC();
        _slotNum              = (short)r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_splitAmount <= 0) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var sourceStorage = _sourceStorageType == 1 ? player.Warehouse : player.Inventory;
        var destStorage   = _destinationStorageType == 1 ? player.Warehouse : player.Inventory;

        var source = sourceStorage.Get(_sourceItemObjId);
        if (source is null || source.IsEquipped || source.Count < _splitAmount) return;

        var target = _destinationItemObjId != 0
            ? destStorage.Get(_destinationItemObjId)
            : null;

        if (target is not null && target.ItemId == source.ItemId)
        {
            // Merge: reject if target would exceed max stack size
            var tpl = _dataManager.Items.GetTemplate(source.ItemId);
            if (tpl is not null && target.Count + _splitAmount > tpl.MaxStackCount) return;

            source.Count -= _splitAmount;
            target.Count += _splitAmount;

            await SaveStoragesAsync(player, _sourceStorageType, _destinationStorageType, ct);
            await SendUpdatesAsync(player, [source], _sourceStorageType, ct);
            await SendUpdatesAsync(player, [target], _destinationStorageType, ct);
        }
        else
        {
            // Split: create new item with splitAmount at slotNum; source may reach zero (full-stack move)
            source.Count -= _splitAmount;

            long uid = await _itemDao.NextUniqueIdAsync(ct);
            var newItem = new Item
            {
                UniqueId    = uid,
                ItemId      = source.ItemId,
                Count       = _splitAmount,
                Slot        = _slotNum,
                StorageType = _destinationStorageType,
            };
            destStorage.Add(newItem);

            if (source.Count == 0)
            {
                sourceStorage.Remove(source.UniqueId);
                await _itemDao.DeleteAsync(source.UniqueId, ct);

                // If source and dest are the same storage, send one update; otherwise two
                if (_sourceStorageType == _destinationStorageType)
                {
                    await SaveStoragesAsync(player, _sourceStorageType, _destinationStorageType, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM(source.UniqueId), ct);
                    await SendUpdatesAsync(player, [newItem], _destinationStorageType, ct);
                }
                else
                {
                    await SaveStoragesAsync(player, _sourceStorageType, _destinationStorageType, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM(source.UniqueId), ct);
                    await SendStorageRefreshAsync(player, _sourceStorageType, ct);
                    await SendUpdatesAsync(player, [newItem], _destinationStorageType, ct);
                }
            }
            else
            {
                await SaveStoragesAsync(player, _sourceStorageType, _destinationStorageType, ct);
                await SendUpdatesAsync(player, [source], _sourceStorageType, ct);
                await SendUpdatesAsync(player, [newItem], _destinationStorageType, ct);
            }
        }
    }

    private async Task SaveStoragesAsync(Model.Player player, byte src, byte dst, CancellationToken ct)
    {
        if (src == 0 || dst == 0)
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        if (src == 1 || dst == 1)
            await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);
    }

    private async Task SendUpdatesAsync(Model.Player player, Item[] items, byte storageType, CancellationToken ct)
    {
        if (storageType == 1)
            await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
        else
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(items), ct);
    }

    private async Task SendStorageRefreshAsync(Model.Player player, byte storageType, CancellationToken ct)
    {
        if (storageType == 1)
            await _conn.SendAsync(new SM_WAREHOUSE_INFO(player.Warehouse.All), ct);
        // inventory deletions are sent via SM_DELETE_ITEM by the caller
    }
}
