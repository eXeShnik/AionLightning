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
    // Crafting skill advances while recipe.SkillPoint is within this margin of player's current skill
    private const int SkillAdvanceWindow = 50;

    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExperienceService        _expService;
    private readonly SkillLearnService        _skillLearn;
    private readonly QuestService             _questService;
    private readonly AionLightning.Game.QuestEngine.QuestEngine _questEngine;

    private int _unk;
    private int _targetTemplateId;
    private int _recipeId;
    private int _targetObjId;
    private int _materialsCount;
    private int _craftType;

    public CM_CRAFT(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager,
        PlayerConnectionRegistry connRegistry, ExperienceService expService, SkillLearnService skillLearn,
        QuestService questService, AionLightning.Game.QuestEngine.QuestEngine questEngine)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _connRegistry = connRegistry;
        _expService   = expService;
        _skillLearn   = skillLearn;
        _questService = questService;
        _questEngine  = questEngine;
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

        // Success/fail roll — simplified from Java CraftingTask multi-tick bar fill.
        // skillLvlDiff >= 0 is guaranteed by the gate above; success climbs from 50% at margin 0 to 95% at margin 22+.
        int skillLvlDiff  = recipe.SkillId > 0 ? player.Skills.GetLevel(recipe.SkillId) - recipe.SkillPoint : 50;
        int successChance = Math.Clamp(50 + skillLvlDiff * 2, 5, 95);
        bool craftSuccess = Random.Shared.Next(100) < successChance;

        // Critical craft roll — 15% base (mirrors Java CraftConfig.CRAFT_CRIT_RATE default), only if recipe has a combo product.
        bool isCrit     = craftSuccess && recipe.ComboProductId > 0 && Random.Shared.Next(100) < 15;
        int  productId  = isCrit ? recipe.ComboProductId : recipe.ProductId;

        // Check inventory capacity before consuming any materials — prevents item sink if product cannot fit.
        // A component that is fully consumed frees a slot, so we must net the freed slots.
        // On fail we skip this check since no product slot is needed.
        if (craftSuccess)
        {
            int slotsFreed = recipe.Components.Count(c =>
            {
                var item = player.Inventory.FindByItemId(c.ItemId);
                return item is not null && item.Count <= c.Quantity;
            });
            var productTemplate = _dataManager.Items.GetTemplate(productId);
            bool productStacks  = productTemplate is { MaxStackCount: > 1 }
                                  && player.Inventory.FindByItemId(productId) is not null;
            if (!productStacks && (player.Inventory.BagSlotUsed - slotsFreed) >= player.Inventory.Capacity)
            {
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
                return;
            }
        }

        int skillId = recipe.SkillId;
        int worldId = player.Position.WorldId;

        // Broadcast craft start animation
        var startAnim = new SM_CRAFT_ANIMATION(player.ObjectId, _targetObjId, skillId, 1);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(startAnim, ct); } catch { }

        // Send init update (shows craft window / progress bar)
        await _conn.SendAsync(new SM_CRAFT_UPDATE(skillId, recipe.ProductId, recipe.NameId, 100, 100, 0), ct);

        // Consume components — always, even on fail (mirrors Java: materials consumed in checkCraft before task starts)
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
        foreach (var consumed in partiallyConsumed)
            try { await _conn.SendAsync(new SM_INVENTORY_UPDATE_ITEM(consumed, SM_INVENTORY_UPDATE_ITEM.UpdateType.DecItemUse), ct); } catch { }

        if (!craftSuccess)
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_CRAFT_UPDATE(skillId, recipe.ProductId, recipe.NameId, 0, 100, 6), ct);
            var failAnim = new SM_CRAFT_ANIMATION(player.ObjectId, _targetObjId, skillId, 2);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(failAnim, ct); } catch { }
            await _questEngine.OnFailCraftAsync(player, recipe.ProductId, _conn, ct); // Java onFailCraft
            return;
        }

        // Critical feedback — send blue-crit update before delivering the combo product
        if (isCrit)
            await _conn.SendAsync(new SM_CRAFT_UPDATE(skillId, productId, recipe.NameId, 100, 0, 2), ct);

        // Create product
        long uid    = await _itemDao.NextUniqueIdAsync(ct);
        var product = new Item { UniqueId = uid, ItemId = productId, Count = recipe.Quantity, Slot = -1 };
        player.Inventory.Add(product);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([product]), ct);

        // Check quest collect-item progress for the crafted product
        await _questService.HandleItemAcquiredAsync(player, productId, _conn, ct);

        // Award crafting XP based on recipe skill point requirement
        await _expService.AddCraftingExpAsync(player, recipe.SkillPoint, _conn, ct);

        // Advance crafting skill if still learning from this recipe (within SkillAdvanceWindow)
        if (recipe.SkillId > 0)
        {
            int currentSkillLevel = player.Skills.GetLevel(recipe.SkillId);
            if (currentSkillLevel > 0 && currentSkillLevel < recipe.SkillPoint + SkillAdvanceWindow)
                await _skillLearn.LearnSkillAsync(player, recipe.SkillId, currentSkillLevel + 1, ct: ct);
        }

        // Success craft update + stop animation (broadcast)
        await _conn.SendAsync(new SM_CRAFT_UPDATE(skillId, productId, recipe.NameId, 100, 0, 5), ct);

        var stopAnim = new SM_CRAFT_ANIMATION(player.ObjectId, _targetObjId, skillId, 2);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(stopAnim, ct); } catch { }
    }
}
