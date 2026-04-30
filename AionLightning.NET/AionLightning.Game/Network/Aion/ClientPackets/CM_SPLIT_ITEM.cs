using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Player splits a stack into two, or merges it into an existing same-item stack. Opcode 0x17F.
/// Only inventory-to-inventory (sourceStorageType=0, destinationStorageType=0) is handled.
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
        // Warehouse moves not yet implemented
        if (_sourceStorageType != 0 || _destinationStorageType != 0) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var source = player.Inventory.Get(_sourceItemObjId);
        if (source is null || source.IsEquipped || source.Count < _splitAmount) return;

        var target = _destinationItemObjId != 0
            ? player.Inventory.Get(_destinationItemObjId)
            : null;

        if (target is not null && target.ItemId == source.ItemId)
        {
            // Merge: reject if target would exceed max stack size
            var tpl = _dataManager.Items.GetTemplate(source.ItemId);
            if (tpl is not null && target.Count + _splitAmount > tpl.MaxStackCount) return;

            source.Count -= _splitAmount;
            target.Count += _splitAmount;

            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([source, target]), ct);
        }
        else
        {
            // Split: create new item with splitAmount at slotNum; source may reach zero (full-stack move)
            source.Count -= _splitAmount;

            long uid = await _itemDao.NextUniqueIdAsync(ct);
            var newItem = new Item { UniqueId = uid, ItemId = source.ItemId, Count = _splitAmount, Slot = _slotNum };
            player.Inventory.Add(newItem);

            if (source.Count == 0)
            {
                player.Inventory.Remove(source.UniqueId);
                await _itemDao.DeleteAsync(source.UniqueId, ct);
                await _conn.SendAsync(new SM_DELETE_ITEM(source.UniqueId), ct);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([newItem]), ct);
            }
            else
            {
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([source, newItem]), ct);
            }
        }
    }
}
