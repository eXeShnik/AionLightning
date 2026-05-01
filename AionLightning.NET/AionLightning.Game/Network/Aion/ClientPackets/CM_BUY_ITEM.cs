using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client buys from or sells to an NPC shop, or buys from a player's private store. Opcode 0xF1.</summary>
public sealed class CM_BUY_ITEM : AionClientPacket
{
    private const int   KinahItemId       = 182400001;
    private const int   SellPriceDivisor  = 2;    // players get 50% of item price when selling
    private const float MaxInteractRange  = 10.0f; // lenient for latency; retail NPC interaction is ~5m

    private readonly GsClientConnection       _conn;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly GameWorld                _world;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _sellerObjectId;
    private int _tradeActionId;
    private readonly List<(int ItemId, long Count)> _tradeEntries = new();
    private bool _invalid;

    public CM_BUY_ITEM(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager,
        GameWorld world, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _world        = world;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _sellerObjectId = r.ReadD();
        _tradeActionId  = r.ReadH();
        int amount      = r.ReadH();

        if (amount is < 0 or > 36) { _invalid = true; return; }

        for (int i = 0; i < amount; i++)
        {
            int  itemId = r.ReadD();
            long count  = r.ReadQ();
            if (count <= 0 || count > 20000 || itemId <= 0) { _invalid = true; return; }
            _tradeEntries.Add((itemId, count));
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_invalid) return;
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_tradeActionId)
        {
            case 0:  // buy from player's private store
                await BuyFromPrivateStoreAsync(player, ct);
                break;
            case 13: // buy from normal NPC shop
                await BuyFromShopAsync(player, ct);
                break;
            case 1:  // sell items to NPC shop
                await SellToShopAsync(player, ct);
                break;
        }
    }

    private async ValueTask BuyFromShopAsync(Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_sellerObjectId);
        if (npc is null) return;
        if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        // Single-pass: filter to items that can be received and accumulate cost.
        // CanReceive is evaluated in order so that each accepted item correctly
        // consumes a simulated slot before the next item is checked.
        var purchasePlan = new List<(int ItemId, long Count, long Cost, int MaxStack)>();
        var simulatedInventory = player.Inventory; // checks run against live inventory first
        int simulatedSlots = simulatedInventory.BagSlotUsed;

        foreach (var (itemId, count) in _tradeEntries)
        {
            if (!_dataManager.Shop.NpcSellsItem(npc.Template.NpcId, itemId)) continue;
            var template = _dataManager.Items.GetTemplate(itemId);
            if (template is null) continue;

            // Capacity check: stackable items that already have a stack don't consume a slot.
            bool hasExisting = simulatedInventory.FindByItemId(itemId) is not null;
            bool canStack    = hasExisting && template.MaxStackCount > 1;
            if (!canStack)
            {
                if (simulatedSlots >= simulatedInventory.Capacity) continue; // full
                simulatedSlots++;
            }

            purchasePlan.Add((itemId, count, template.Price * count, template.MaxStackCount));
        }

        if (purchasePlan.Count == 0) return;

        long totalCost = purchasePlan.Sum(p => p.Cost);

        // Check player has enough kinah
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        long currentKinah = kinahItem?.Count ?? 0;
        if (currentKinah < totalCost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        // Deduct kinah in memory
        if (kinahItem is not null)
            kinahItem.Count -= totalCost;

        // Add purchased items (always fit because plan was validated above)
        var itemsAdded = new List<Item>();
        foreach (var (itemId, count, _, maxStack) in purchasePlan)
        {
            var existing = player.Inventory.FindByItemId(itemId);
            if (existing is not null && maxStack > 1)
            {
                existing.Count += count;
                itemsAdded.Add(existing);
            }
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                var newItem = new Item { UniqueId = uid, ItemId = itemId, Count = count, Slot = -1 };
                player.Inventory.Add(newItem);
                itemsAdded.Add(newItem);
            }
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        if (kinahItem is not null)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
        if (itemsAdded.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(itemsAdded), ct);
    }

    private async ValueTask SellToShopAsync(Player player, CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_sellerObjectId);
        if (npc is null || player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        long kinahGained = 0;
        var itemsToDelete = new List<long>();
        var itemsToUpdate = new List<Item>();

        foreach (var (itemId, count) in _tradeEntries)
        {
            var template = _dataManager.Items.GetTemplate(itemId);
            long sellPrice = (template?.Price ?? 0) / SellPriceDivisor;

            var item = player.Inventory.FindByItemId(itemId);
            if (item is null || item.IsEquipped) continue;

            long sellCount = Math.Min(count, item.Count);
            kinahGained += sellPrice * sellCount;
            item.Count -= sellCount;

            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                itemsToDelete.Add(item.UniqueId);
            }
            else
            {
                itemsToUpdate.Add(item);
            }
        }

        // Add kinah gained in memory
        Item? kinahItem = null;
        if (kinahGained > 0)
        {
            kinahItem = player.Inventory.FindByItemId(KinahItemId);
            if (kinahItem is null)
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                kinahItem = new Item { UniqueId = uid, ItemId = KinahItemId, Count = kinahGained, Slot = -1 };
                player.Inventory.Add(kinahItem);
            }
            else
            {
                kinahItem.Count += kinahGained;
            }
        }

        // Persist full inventory state (covers deletions, updates, and kinah in one shot)
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        // Notify client of removed items
        foreach (var uid in itemsToDelete)
            await _conn.SendAsync(new SM_DELETE_ITEM(uid), ct);

        if (itemsToUpdate.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(itemsToUpdate), ct);
        if (kinahItem is not null)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
    }

    private async ValueTask BuyFromPrivateStoreAsync(Player buyer, CancellationToken ct)
    {
        var seller = _world.GetPlayerByObjectId(_sellerObjectId);
        if (seller?.StoreItems is null) return;
        if ((seller.State & CreatureState.PrivateShop) == 0) return;

        // Match each requested itemId to a store listing; validate stock
        var plan = new List<(PrivateStoreItem StoreItem, long Count, long LineCost)>();
        foreach (var (itemId, count) in _tradeEntries)
        {
            var si = seller.StoreItems.FirstOrDefault(s => s.ItemId == itemId);
            if (si is null || si.Count < count) return;
            plan.Add((si, count, (long)si.Price * count));
        }
        if (plan.Count == 0) return;

        long totalCost = plan.Sum(p => p.LineCost);

        var buyerKinah = buyer.Inventory.FindByItemId(KinahItemId);
        if ((buyerKinah?.Count ?? 0) < totalCost)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        // Inventory space: count slots needed for items the buyer doesn't already have
        int slotsNeeded = plan.Count(p => buyer.Inventory.FindByItemId(p.StoreItem.ItemId) is null
                                          || (_dataManager.Items.GetTemplate(p.StoreItem.ItemId)?.MaxStackCount ?? 1) <= 1);
        if (buyer.Inventory.BagSlotUsed + slotsNeeded > buyer.Inventory.Capacity)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
            return;
        }

        var buyerChanged  = new List<Item>();
        var sellerChanged = new List<Item>();
        var sellerDeleted = new List<long>();

        foreach (var (si, qty, _) in plan)
        {
            // Decrease seller's actual item
            var sellerItem = seller.Inventory.Get(si.UniqueId);
            if (sellerItem is null) return;
            sellerItem.Count -= qty;
            if (sellerItem.Count <= 0)
            {
                seller.Inventory.Remove(sellerItem.UniqueId);
                sellerDeleted.Add(sellerItem.UniqueId);
                seller.StoreItems.RemoveAll(s => s.UniqueId == si.UniqueId);
            }
            else
            {
                sellerChanged.Add(sellerItem);
                int idx = seller.StoreItems.FindIndex(s => s.UniqueId == si.UniqueId);
                if (idx >= 0) seller.StoreItems[idx] = si with { Count = (int)sellerItem.Count };
            }

            // Add item to buyer
            var template = _dataManager.Items.GetTemplate(si.ItemId);
            var existing = buyer.Inventory.FindByItemId(si.ItemId);
            if (existing is not null && (template?.MaxStackCount ?? 1) > 1)
            {
                existing.Count += qty;
                buyerChanged.Add(existing);
            }
            else
            {
                long uid    = await _itemDao.NextUniqueIdAsync(ct);
                var newItem = new Item { UniqueId = uid, ItemId = si.ItemId, Count = qty, Slot = -1 };
                buyer.Inventory.Add(newItem);
                buyerChanged.Add(newItem);
            }
        }

        // Transfer kinah
        buyerKinah!.Count -= totalCost;
        var sellerKinah = seller.Inventory.FindByItemId(KinahItemId);
        if (sellerKinah is null)
        {
            long uid = await _itemDao.NextUniqueIdAsync(ct);
            sellerKinah = new Item { UniqueId = uid, ItemId = KinahItemId, Count = totalCost, Slot = -1 };
            seller.Inventory.Add(sellerKinah);
        }
        else
        {
            sellerKinah.Count += totalCost;
        }
        sellerChanged.Add(sellerKinah);

        // Persist both inventories
        await _itemDao.SaveAllAsync(buyer.ObjectId,  buyer.Inventory.All,  ct);
        await _itemDao.SaveAllAsync(seller.ObjectId, seller.Inventory.All, ct);

        // Notify buyer
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([buyerKinah]), ct);
        if (buyerChanged.Count > 0)
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM(buyerChanged), ct);

        // Notify seller
        var sellerConn = _connRegistry.Get(seller.ObjectId);
        if (sellerConn is not null)
        {
            foreach (var uid in sellerDeleted)
                try { await sellerConn.SendAsync(new SM_DELETE_ITEM(uid), ct); } catch { }
            if (sellerChanged.Count > 0)
                try { await sellerConn.SendAsync(new SM_INVENTORY_ADD_ITEM(sellerChanged), ct); } catch { }
        }

        // Auto-close store when all items have sold
        if (seller.StoreItems is { Count: 0 })
        {
            seller.StoreItems = null;
            seller.StoreName  = string.Empty;
            seller.State     &= ~CreatureState.PrivateShop;
            var closeEmotion = new SM_EMOTION(seller, EmotionType.CLOSE_PRIVATESHOP);
            int worldId = seller.Position.WorldId;
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(closeEmotion, ct); } catch { }
        }
    }
}
