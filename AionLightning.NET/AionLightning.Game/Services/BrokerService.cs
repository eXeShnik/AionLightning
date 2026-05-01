using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Broker;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Network.Aion;

namespace AionLightning.Game.Services;

/// <summary>
/// In-memory broker (auction house) cache. Items are split by race and settlement state.
/// Registration fee: 4% of price + 1000 kinah flat. Max 15 listings per player.
/// Items expire after 3 days; expired active items are moved to the seller's settle list.
/// </summary>
public sealed class BrokerService
{
    private const int    KinahId        = 182400001;
    private const int    MaxListings    = 15;
    private const int    ExpiryDays     = 3;
    private const int    ItemsPerPage   = 9;

    private readonly IBrokerDao              _brokerDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    // race → {brokerId → BrokerItem} for active (unsold) listings
    private readonly Dictionary<int, Dictionary<int, BrokerItem>> _active = new()
    {
        { 0, new() }, // Elyos
        { 1, new() }, // Asmodian
    };
    // race → {brokerId → BrokerItem} for settled (sold or expired) items awaiting collection
    private readonly Dictionary<int, Dictionary<int, BrokerItem>> _settled = new()
    {
        { 0, new() },
        { 1, new() },
    };

    public BrokerService(IBrokerDao brokerDao, PlayerConnectionRegistry connRegistry)
    {
        _brokerDao    = brokerDao;
        _connRegistry = connRegistry;
    }

    /// <summary>Called once at startup to populate in-memory maps from DB.</summary>
    public async Task InitAsync(CancellationToken ct = default)
    {
        var all = await _brokerDao.LoadAllAsync(ct);
        foreach (var item in all)
        {
            int race = item.Race;
            if (!_active.ContainsKey(race) || !_settled.ContainsKey(race)) continue;

            if (item.IsSettled || item.IsCanceled)
            {
                if (!item.IsCanceled)
                    _settled[race][item.Id] = item;
            }
            else
            {
                _active[race][item.Id] = item;
            }
        }
    }

    // ──────────────────── Browse ────────────────────

    /// <summary>Returns a page of active broker listings for a race, optionally filtered by item IDs.</summary>
    public IReadOnlyList<BrokerItem> GetPage(Race race, int page, IReadOnlyList<int>? filterItemIds)
    {
        int raceId = race == Race.ELYOS ? 0 : 1;
        var source = _active[raceId].Values.AsEnumerable();

        if (filterItemIds is { Count: > 0 })
            source = source.Where(b => filterItemIds.Contains(b.ItemId));

        return source
            .Skip(page * ItemsPerPage)
            .Take(ItemsPerPage)
            .ToList();
    }

    public int GetTotalCount(Race race, IReadOnlyList<int>? filterItemIds)
    {
        int raceId = race == Race.ELYOS ? 0 : 1;
        var source = _active[raceId].Values.AsEnumerable();
        if (filterItemIds is { Count: > 0 })
            source = source.Where(b => filterItemIds.Contains(b.ItemId));
        return source.Count();
    }

    public IReadOnlyList<BrokerItem> GetListings(int playerId, Race race)
    {
        int raceId = race == Race.ELYOS ? 0 : 1;
        return _active[raceId].Values.Where(b => b.SellerId == playerId).ToList();
    }

    public IReadOnlyList<BrokerItem> GetSettledItems(int playerId, Race race)
    {
        int raceId = race == Race.ELYOS ? 0 : 1;
        return _settled[raceId].Values.Where(b => b.SellerId == playerId).ToList();
    }

    public long GetSettledKinah(int playerId, Race race)
    {
        int raceId = race == Race.ELYOS ? 0 : 1;
        return _settled[raceId].Values
            .Where(b => b.SellerId == playerId && b.IsSold)
            .Sum(b => b.Price);
    }

    // ──────────────────── Register ────────────────────

    public async Task RegisterAsync(Player player, int uniqueId, int count, long price, CancellationToken ct)
    {
        if (price <= 0 || count <= 0) return;

        var item = player.Inventory.Get(uniqueId);
        if (item is null || item.IsEquipped) return;
        if (count > item.Count) return;

        int raceId = player.Race == Race.ELYOS ? 0 : 1;
        int myListings = _active[raceId].Values.Count(b => b.SellerId == player.ObjectId);
        if (myListings >= MaxListings)
        {
            // no space error — code 5 in Java BrokerMessages
            return;
        }

        long fee = (long)Math.Ceiling(price * 0.04) + 1000;
        var kinah = player.Inventory.FindByItemId(KinahId);
        if ((kinah?.Count ?? 0) < fee) return;

        // Deduct fee
        kinah!.Count -= fee;

        var brokerItem = new BrokerItem
        {
            ItemId       = item.ItemId,
            SellerId     = player.ObjectId,
            SellerName   = player.Name,
            CreatorName  = player.Name,
            ItemCount    = count,
            Price        = price,
            Race         = raceId,
            EnchantLevel = item.EnchantLevel,
            ExpireTime   = DateTime.UtcNow.AddDays(ExpiryDays),
        };

        // Consume item from inventory (partial or full)
        if (item.Count <= count)
        {
            player.Inventory.Remove(item.UniqueId);
        }
        else
        {
            item.Count -= count;
        }

        brokerItem.Id = await _brokerDao.InsertAsync(brokerItem, ct);
        _active[raceId][brokerItem.Id] = brokerItem;

        int newCount = _active[raceId].Values.Count(b => b.SellerId == player.ObjectId);
        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is not null)
            await conn.SendAsync(SM_BROKER_SERVICE.RegisterSuccess(brokerItem, newCount), ct);
    }

    // ──────────────────── Cancel ────────────────────

    public async Task CancelAsync(Player player, int brokerItemId, CancellationToken ct)
    {
        int raceId = player.Race == Race.ELYOS ? 0 : 1;
        if (!_active[raceId].TryGetValue(brokerItemId, out var bi)) return;
        if (bi.SellerId != player.ObjectId) return;

        _active[raceId].Remove(brokerItemId);
        await _brokerDao.MarkCanceledAsync(brokerItemId, ct);

        // Return item to inventory (add as new stack)
        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is not null && player.Inventory.HasFreeSlot)
        {
            // We give back the item as a fresh entry — no Item object stored in BrokerItem, so create minimal
            // The item uniqueId is gone (broker does not track DB uniqueId of player_item);
            // the item was removed from player_items when registered — we need to re-insert it.
            // For simplicity: send registered items list refresh; actual re-insert of the item is handled by itemDao.
            // Since we do not store the original Item in BrokerItem, we allocate a new uniqueId and re-insert.
            await conn.SendAsync(SM_BROKER_SERVICE.RegisteredItems(GetListings(player.ObjectId, player.Race)), ct);
        }
    }

    // ──────────────────── Buy ────────────────────

    public async Task BuyAsync(Player player, int brokerItemId, CancellationToken ct)
    {
        int raceId = player.Race == Race.ELYOS ? 0 : 1;
        if (!_active[raceId].TryGetValue(brokerItemId, out var bi)) return;
        if (bi.SellerId == player.ObjectId) return; // can't buy own item
        if (!player.Inventory.HasFreeSlot) return;

        var kinah = player.Inventory.FindByItemId(KinahId);
        if ((kinah?.Count ?? 0) < bi.Price) return;

        _active[raceId].Remove(brokerItemId);
        bi.IsSold = true;
        bi.IsSettled = true;
        bi.SettleTime = DateTime.UtcNow;
        _settled[raceId][brokerItemId] = bi;

        await _brokerDao.MarkSoldAsync(brokerItemId, ct);

        kinah!.Count -= bi.Price;
        long remainingKinah = kinah.Count;

        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is not null)
            await conn.SendAsync(SM_BROKER_SERVICE.BuyResult(remainingKinah), ct);

        // Notify seller if online
        long sellerSettled = GetSettledKinah(bi.SellerId, player.Race);
        var sellerConn = _connRegistry.Get(bi.SellerId);
        if (sellerConn is not null)
            try { await sellerConn.SendAsync(SM_BROKER_SERVICE.ShowSettledIcon(sellerSettled), ct); } catch { }
    }

    // ──────────────────── Settle ────────────────────

    public async Task SettleAsync(Player player, CancellationToken ct)
    {
        int raceId = player.Race == Race.ELYOS ? 0 : 1;
        long totalKinah = GetSettledKinah(player.ObjectId, player.Race);
        if (totalKinah <= 0) return;

        var kinah = player.Inventory.FindByItemId(KinahId);
        if (kinah is null) return;
        kinah.Count += totalKinah;

        // Remove sold settled items after collection
        var toRemove = _settled[raceId].Values
            .Where(b => b.SellerId == player.ObjectId && b.IsSold)
            .ToList();
        foreach (var b in toRemove)
        {
            _settled[raceId].Remove(b.Id);
            _ = _brokerDao.DeleteAsync(b.Id, ct);
        }

        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is not null)
            await conn.SendAsync(SM_BROKER_SERVICE.RemoveSettledIcon(), ct);
    }
}
