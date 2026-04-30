using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client initiates a crafting action. Opcode 0x12F.</summary>
public sealed class CM_CRAFT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;

    private int _unk;
    private int _targetTemplateId;
    private int _recipeId;
    private int _targetObjId;
    private int _materialsCount;
    private int _craftType;

    public CM_CRAFT(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        _unk              = r.ReadC();
        _targetTemplateId = r.ReadD();
        _recipeId         = r.ReadD();
        _targetObjId      = r.ReadD();
        _materialsCount   = r.ReadH();
        _craftType        = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var recipe = _dataManager.Recipes.GetTemplate(_recipeId);
        if (recipe is null) return;

        // Validate player has all components
        foreach (var component in recipe.Components)
        {
            var item = player.Inventory.FindByItemId(component.ItemId);
            if (item is null || item.Count < component.Quantity) return;
        }

        // Consume components; track partial stacks for client update
        var partiallyConsumed = new List<Item>();
        foreach (var component in recipe.Components)
        {
            var item = player.Inventory.FindByItemId(component.ItemId);
            if (item is null) return;

            item.Count -= component.Quantity;
            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                await _itemDao.DeleteAsync(item.UniqueId, ct);
                await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
            }
            else
            {
                partiallyConsumed.Add(item);
            }
        }

        // Create product item
        var uniqueId = await _itemDao.NextUniqueIdAsync(ct);
        var product  = new Item
        {
            UniqueId = uniqueId,
            ItemId   = recipe.ProductId,
            Count    = recipe.Quantity,
            Slot     = -1,
        };
        player.Inventory.Add(product);

        // Persist full inventory state
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify client: push partial-stack updates first, then the new product
        if (partiallyConsumed.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumed), ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([product]), ct);
    }
}
