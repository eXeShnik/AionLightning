namespace AionLightning.Game.Model.LimitedItems;

/// <summary>
/// Port of Java <c>model.limiteditems.LimitedItem</c> — per-NPC, per-item limited-stock state declared
/// via a goodslist's sell_limit/buy_limit attributes.
/// TYPE_A: BuyLimit == 0 &amp;&amp; SellLimit != 0 (finite global stock only, shared by all buyers).
/// TYPE_B: BuyLimit != 0 &amp;&amp; SellLimit == 0 (per-player purchase cap only, no global stock cap).
/// TYPE_C: BuyLimit != 0 &amp;&amp; SellLimit != 0 (both apply at once).
/// Java tracked buy counts in a shared <c>TIntObjectHashMap</c> without locking individual field
/// mutations; this port guards the whole check-and-decrement with a lock so concurrent buyers across
/// connections can't over-decrement stock.
/// </summary>
public sealed class LimitedItem
{
    private readonly object _gate = new();
    private readonly Dictionary<int, long> _buyCounts = new();

    public int ItemId { get; }
    public int DefaultSellLimit { get; }
    public int BuyLimit { get; }
    public string? SalesTime { get; }
    public long SellLimit { get; private set; }

    /// <summary>Remaining global stock, or null if this item has no finite-stock cap (unlimited stock,
    /// only a per-player purchase cap applies).</summary>
    public long? Remaining => DefaultSellLimit != 0 ? SellLimit : null;

    public LimitedItem(int itemId, int sellLimit, int buyLimit, string? salesTime)
    {
        ItemId = itemId;
        SellLimit = sellLimit;
        DefaultSellLimit = sellLimit;
        BuyLimit = buyLimit;
        SalesTime = salesTime;
    }

    /// <summary>
    /// Port of the TYPE_A/B/C check-and-decrement inlined in Java's
    /// <c>TradeService.performBuyFromShop</c>, made atomic. Unlike Java (which aborts the whole batch
    /// purchase with a bare <c>return false</c> the moment any single item exceeds its limit), this
    /// clamps to whatever is actually available so a partially-stocked item still sells instead of
    /// blocking the rest of the purchase. Returns the count actually reserved (0 if sold out or the
    /// player's purchase cap is already exhausted).
    /// </summary>
    public long TryReserve(int playerObjectId, long requestedCount)
    {
        if (requestedCount <= 0) return 0;

        lock (_gate)
        {
            bool hasStockCap = DefaultSellLimit != 0;
            bool hasPlayerCap = BuyLimit != 0;

            long allowed = requestedCount;
            if (hasStockCap)
                allowed = Math.Min(allowed, SellLimit);
            if (hasPlayerCap)
            {
                _buyCounts.TryGetValue(playerObjectId, out long bought);
                allowed = Math.Min(allowed, BuyLimit - bought);
            }

            if (allowed <= 0) return 0;

            if (hasStockCap)
                SellLimit -= allowed;
            if (hasPlayerCap)
            {
                _buyCounts.TryGetValue(playerObjectId, out long bought);
                _buyCounts[playerObjectId] = bought + allowed;
            }

            return allowed;
        }
    }

    /// <summary>Reverses a <see cref="TryReserve"/> reservation because the overall purchase (kinah
    /// check, inventory capacity, etc.) failed after stock was already reserved.</summary>
    public void Release(int playerObjectId, long count)
    {
        if (count <= 0) return;

        lock (_gate)
        {
            if (DefaultSellLimit != 0)
                SellLimit = Math.Min(SellLimit + count, DefaultSellLimit);
            if (BuyLimit != 0 && _buyCounts.TryGetValue(playerObjectId, out long bought))
                _buyCounts[playerObjectId] = Math.Max(0, bought - count);
        }
    }

    /// <summary>Java <c>LimitedItem.setToDefault()</c> — cron-driven restock: global stock refills to
    /// its starting value and every player's purchase count clears.</summary>
    public void ResetToDefault()
    {
        lock (_gate)
        {
            SellLimit = DefaultSellLimit;
            _buyCounts.Clear();
        }
    }
}
