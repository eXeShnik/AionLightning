using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client confirms item selection from a reward box dialog. Opcode 0x18E.</summary>
public sealed class CM_SELECTITEM_OK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;

    private int _uniqueItemId;
    private int _index;

    public CM_SELECTITEM_OK(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        _uniqueItemId = r.ReadD();
        r.ReadD();       // unk
        _index        = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var box = player.Inventory.Get(_uniqueItemId);
        if (box is null) return;

        var template    = _dataManager.Items.GetTemplate(box.ItemId);
        if (template is null || !template.IsSelectableBox) return;

        var selectItems = _dataManager.SelectItems.GetSelectItems(player.PlayerClass, box.ItemId);
        if (selectItems is null || _index < 0 || _index >= selectItems.Items.Count) return;

        var choice = selectItems.Items[_index];

        // Consume the box (non-stackable — count always 1)
        player.Inventory.Remove(box.UniqueId);
        await _itemDao.DeleteAsync(box.UniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(box.UniqueId), ct);

        // Give the chosen item
        long uid     = await _itemDao.NextUniqueIdAsync(ct);
        var  newItem = new Item { UniqueId = uid, ItemId = choice.Id, Count = choice.Count, Slot = -1 };
        player.Inventory.Add(newItem);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([newItem]), ct);

        await _conn.SendAsync(new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)box.UniqueId, box.ItemId), ct);
        await _conn.SendAsync(new SM_SELECT_ITEM_ADD((int)box.UniqueId, _index), ct);
    }
}
