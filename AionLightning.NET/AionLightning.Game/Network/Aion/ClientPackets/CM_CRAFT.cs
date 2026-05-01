using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client initiates a crafting action. Opcode 0x12F.</summary>
public sealed class CM_CRAFT : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExperienceService        _expService;

    private int _unk;
    private int _targetTemplateId;
    private int _recipeId;
    private int _targetObjId;
    private int _materialsCount;
    private int _craftType;

    public CM_CRAFT(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager,
        PlayerConnectionRegistry connRegistry, ExperienceService expService)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
        _expService   = expService;
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

        // Player must know the recipe
        if (!player.KnownRecipes.Contains(_recipeId)) return;

        // Craft skill gate — player must have the required crafting skill at sufficient level
        if (recipe.SkillId > 0)
        {
            if (!player.Skills.IsPresent(recipe.SkillId)) return;
            if (player.Skills.GetLevel(recipe.SkillId) < recipe.SkillPoint) return;
        }

        // Validate player has all components
        foreach (var component in recipe.Components)
        {
            var item = player.Inventory.FindByItemId(component.ItemId);
            if (item is null || item.Count < component.Quantity) return;
        }

        // Check inventory capacity before consuming any materials — prevents item sink if product cannot fit.
        // A component that is fully consumed frees a slot, so we must net the freed slots.
        int slotsFreed = recipe.Components.Count(c =>
        {
            var item = player.Inventory.FindByItemId(c.ItemId);
            return item is not null && item.Count <= c.Quantity; // will be fully consumed → frees a slot
        });
        var productTemplate = _dataManager.Items.GetTemplate(recipe.ProductId);
        bool productStacks  = productTemplate is { MaxStackCount: > 1 }
                              && player.Inventory.FindByItemId(recipe.ProductId) is not null;
        if (!productStacks && (player.Inventory.BagSlotUsed - slotsFreed) >= player.Inventory.Capacity)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
            return;
        }

        int skillId = recipe.SkillId;
        int worldId = player.Position.WorldId;

        // Broadcast craft start animation
        var startAnim = new SM_CRAFT_ANIMATION(player.ObjectId, _targetObjId, skillId, 1);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(startAnim, ct); } catch { }

        // Send init update (shows craft window / progress bar)
        await _conn.SendAsync(new SM_CRAFT_UPDATE(skillId, recipe.ProductId, 0, 100, 0, 0), ct);

        // Consume components
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

        // Create product
        long uid    = await _itemDao.NextUniqueIdAsync(ct);
        var product = new Item { UniqueId = uid, ItemId = recipe.ProductId, Count = recipe.Quantity, Slot = -1 };
        player.Inventory.Add(product);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify client of consumed partial stacks and new product
        if (partiallyConsumed.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumed), ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([product]), ct);

        // Award crafting XP based on recipe skill point requirement
        await _expService.AddCraftingExpAsync(player, recipe.SkillPoint, _conn, ct);

        // Success craft update + stop animation (broadcast)
        await _conn.SendAsync(new SM_CRAFT_UPDATE(skillId, recipe.ProductId, 0, 100, 0, 5), ct);

        var stopAnim = new SM_CRAFT_ANIMATION(player.ObjectId, _targetObjId, skillId, 2);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(stopAnim, ct); } catch { }
    }
}
