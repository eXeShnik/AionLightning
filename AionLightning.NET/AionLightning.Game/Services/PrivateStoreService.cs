using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Trade;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Player personal shops ("private stores") — port of Java services.PrivateStoreService.
/// A store is created the first time its owner submits a valid item list and lives on
/// <see cref="Player.Store"/> until explicitly closed (sold out, cancelled, or by the owner).
/// Item/kinah movement reuses the same validate-then-move-then-persist shape as
/// ExchangeService/CM_EXCHANGE_LOCK and BrokerService, so trades here can't dupe or drop items.
/// </summary>
public sealed class PrivateStoreService
{
    private const int KinahItemId = 182400001;

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IItemDao                 _itemDao;
    private readonly IDataManager             _dataManager;
    private readonly GameWorld                _world;

    public PrivateStoreService(PlayerConnectionRegistry connRegistry, IItemDao itemDao,
        IDataManager dataManager, GameWorld world)
    {
        _connRegistry = connRegistry;
        _itemDao      = itemDao;
        _dataManager  = dataManager;
        _world        = world;
    }

    public enum SetItemsResult
    {
        Success,
        InvalidItem,
        // note: Java also rejects items that fail Item.isTradeable() (soulbound/no-trade template flags).
        // The .NET Item/ItemTemplate model has no such flag yet (see ExchangeService/CM_EXCHANGE_ADD_ITEM,
        // which only checks IsEquipped too) — mirrored here for parity; revisit once soulbind is modeled.
        NotTradeable,
    }

    /// <summary>
    /// Validates and (re)populates the owner's store item list — Java PrivateStoreService.addItems
    /// (+ implicit createStore). Does not touch the shop name or broadcast anything; the item-list
    /// packet (CM_PRIVATE_STORE) and the name packet (CM_PRIVATE_STORE_NAME) are sent separately by
    /// the client, mirroring Java's split between addItems() and openPrivateStore().
    /// </summary>
    public SetItemsResult SetItems(Player player, IReadOnlyList<PrivateStoreItem> items)
    {
        var validated = new List<PrivateStoreItem>(items.Count);
        foreach (var si in items)
        {
            var inv = player.Inventory.Get(si.UniqueId);
            if (inv is null || inv.ItemId != si.ItemId || inv.Count < si.Count || si.Price <= 0)
                return SetItemsResult.InvalidItem;
            if (inv.IsEquipped)
                return SetItemsResult.NotTradeable;
            validated.Add(si);
        }

        player.Store ??= new PrivateStore(player);
        player.Store.Items.Clear();
        player.Store.Items.AddRange(validated);
        player.State |= CreatureState.PrivateShop;
        return SetItemsResult.Success;
    }

    /// <summary>
    /// Sets the shop's display name, ensures it is marked open, and broadcasts the name label
    /// (SM_PRIVATE_STORE_NAME) plus the open emotion to every player in the owner's world —
    /// Java PrivateStoreService.openPrivateStore. The open emotion is only sent the first time
    /// the shop opens in this session (Java always resends the emotion on rename too; harmless
    /// either way, but skipping the repeat keeps zone chatter down).
    /// </summary>
    public async ValueTask OpenStoreAsync(Player player, string name, CancellationToken ct)
    {
        bool wasOpen = (player.State & CreatureState.PrivateShop) != 0;

        player.Store ??= new PrivateStore(player);
        player.Store.Name = name;
        player.State     |= CreatureState.PrivateShop;

        int worldId = player.Position.WorldId;
        var namePacket = new SM_PRIVATE_STORE_NAME(player.ObjectId, name);

        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try
            {
                if (!wasOpen)
                    await conn.SendAsync(new SM_EMOTION(player, EmotionType.OPEN_PRIVATESHOP), ct);
                await conn.SendAsync(namePacket, ct);
            }
            catch { }
        }
    }

    /// <summary>Closes the store and broadcasts the close emotion — Java PrivateStoreService.closePrivateStore.</summary>
    public async ValueTask CloseStoreAsync(Player player, CancellationToken ct)
    {
        if ((player.State & CreatureState.PrivateShop) == 0) return;

        player.Store  = null;
        player.State &= ~CreatureState.PrivateShop;

        int worldId = player.Position.WorldId;
        var closeEmotion = new SM_EMOTION(player, EmotionType.CLOSE_PRIVATESHOP);
        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try { await conn.SendAsync(closeEmotion, ct); } catch { }
        }
    }

    /// <summary>Sends the owner's current listing (SM_PRIVATE_STORE) to a viewer — triggered by the
    /// viewer's dialog-select on the shop owner (Java PlayerController.onDialogSelect, dialogId 2).</summary>
    public async ValueTask GetStoreListAsync(Player viewer, int ownerObjectId, CancellationToken ct)
    {
        var owner = _world.GetPlayerByObjectId(ownerObjectId);
        if (owner?.Store is null || (owner.State & CreatureState.PrivateShop) == 0) return;

        var viewerConn = _connRegistry.Get(viewer.ObjectId);
        if (viewerConn is not null)
            await viewerConn.SendAsync(new SM_PRIVATE_STORE(owner), ct);
    }

    /// <summary>
    /// Buys one or more listed items from an open store — Java PrivateStoreService.sellStoreItem.
    /// Validates stock + buyer kinah atomically before mutating either inventory, moves items
    /// owner→buyer and kinah buyer→owner, persists both inventories, and auto-closes the store
    /// once every listed item has sold out.
    /// </summary>
    public async ValueTask BuyFromStoreAsync(Player buyer, int ownerObjectId,
        IReadOnlyList<(int ItemId, long Count)> purchases, CancellationToken ct)
    {
        var seller = _world.GetPlayerByObjectId(ownerObjectId);
        if (seller?.Store is null || (seller.State & CreatureState.PrivateShop) == 0) return;
        if (seller.ObjectId == buyer.ObjectId) return;

        var buyerConn = _connRegistry.Get(buyer.ObjectId);

        // Match each requested itemId to a store listing; validate stock up front.
        var plan = new List<(PrivateStoreItem StoreItem, long Count, long LineCost)>();
        foreach (var (itemId, count) in purchases)
        {
            var si = seller.Store.Items.FirstOrDefault(s => s.ItemId == itemId);
            if (si is null || si.Count < count) return;
            plan.Add((si, count, (long)si.Price * count));
        }
        if (plan.Count == 0) return;

        long totalCost = plan.Sum(p => p.LineCost);

        var buyerKinah = buyer.Inventory.FindByItemId(KinahItemId);
        if ((buyerKinah?.Count ?? 0) < totalCost)
        {
            if (buyerConn is not null)
                await buyerConn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        // Inventory space: count slots needed for items the buyer doesn't already have.
        int slotsNeeded = plan.Count(p => buyer.Inventory.FindByItemId(p.StoreItem.ItemId) is null
                                          || (_dataManager.Items.GetTemplate(p.StoreItem.ItemId)?.MaxStackCount ?? 1) <= 1);
        if (buyer.Inventory.BagSlotUsed + slotsNeeded > buyer.Inventory.Capacity)
        {
            if (buyerConn is not null)
                await buyerConn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
            return;
        }

        var buyerChanged  = new List<Item>();
        var sellerChanged = new List<Item>();
        var sellerDeleted = new List<long>();

        foreach (var (si, qty, _) in plan)
        {
            // Decrease seller's actual item.
            var sellerItem = seller.Inventory.Get(si.UniqueId);
            if (sellerItem is null) return; // item vanished mid-purchase (e.g. concurrent sale) — abort
            sellerItem.Count -= qty;
            if (sellerItem.Count <= 0)
            {
                seller.Inventory.Remove(sellerItem.UniqueId);
                sellerDeleted.Add(sellerItem.UniqueId);
                seller.Store.Items.RemoveAll(s => s.UniqueId == si.UniqueId);
            }
            else
            {
                sellerChanged.Add(sellerItem);
                int idx = seller.Store.Items.FindIndex(s => s.UniqueId == si.UniqueId);
                if (idx >= 0) seller.Store.Items[idx] = si with { Count = (int)sellerItem.Count };
            }

            // Add item to buyer.
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

        // Transfer kinah.
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

        // Persist both inventories.
        await _itemDao.SaveAllAsync(buyer.ObjectId,  buyer.Inventory.All,  ct);
        await _itemDao.SaveAllAsync(seller.ObjectId, seller.Inventory.All, ct);

        // Notify buyer.
        if (buyerConn is not null)
        {
            await buyerConn.SendAsync(new SM_INVENTORY_ADD_ITEM([buyerKinah]), ct);
            if (buyerChanged.Count > 0)
                await buyerConn.SendAsync(new SM_INVENTORY_ADD_ITEM(buyerChanged), ct);
        }

        // Notify seller.
        var sellerConn = _connRegistry.Get(seller.ObjectId);
        if (sellerConn is not null)
        {
            foreach (var uid in sellerDeleted)
                try { await sellerConn.SendAsync(new SM_DELETE_ITEM(uid), ct); } catch { }
            if (sellerChanged.Count > 0)
                try { await sellerConn.SendAsync(new SM_INVENTORY_ADD_ITEM(sellerChanged), ct); } catch { }
        }

        // Auto-close store when all items have sold — Java: getSoldItems().size() == 0 → closePrivateStore.
        if (seller.Store.Items.Count == 0)
            await CloseStoreAsync(seller, ct);
    }
}
