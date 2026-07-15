using AionLightning.Commons.Services;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.LimitedItems;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.LimitedItemTradeService</c> — tracks limited-stock NPC vendor items
/// (finite global stock and/or per-player purchase caps, declared via the sell_limit/buy_limit
/// attributes in goodslists.xml) and restocks them on the goodslist's cron <c>salestime</c>. State is
/// in-memory only, exactly like Java — a server restart implicitly restocks everything.
/// </summary>
public sealed class LimitedItemTradeService
{
    private readonly IDataManager _dataManager;
    private readonly CronService _cronService;
    private readonly ILogger<LimitedItemTradeService> _log;
    private readonly Dictionary<int, LimitedTradeNpc> _limitedTradeNpcs = new();

    public LimitedItemTradeService(IDataManager dataManager, CronService cronService, ILogger<LimitedItemTradeService> log)
    {
        _dataManager = dataManager;
        _cronService = cronService;
        _log = log;
    }

    /// <summary>Java <c>LimitedItemTradeService.start()</c> — builds the per-NPC limited-item sets from
    /// the loaded trade/goods lists, then arms one cron restock per limited item. Called once at boot by
    /// <see cref="LimitedItemTradeServiceHostedService"/>, after the shared Quartz scheduler is running.</summary>
    public async Task StartAsync()
    {
        foreach (var npcId in _dataManager.Shop.NpcIdsWithTradeList)
        {
            foreach (var goodsListId in _dataManager.Shop.GetGoodsListIds(npcId) ?? Array.Empty<int>())
            {
                var goodsList = _dataManager.Shop.GetGoodsList(goodsListId);
                if (goodsList is null)
                {
                    _log.LogWarning("LimitedItemTradeService: no goodslist for tradelist of npc {NpcId}", npcId);
                    continue;
                }

                var limitedItems = goodsList.GetLimitedItems();
                if (limitedItems.Count == 0) continue;

                if (_limitedTradeNpcs.TryGetValue(npcId, out var existing))
                    existing.AddLimitedItems(limitedItems);
                else
                    _limitedTradeNpcs[npcId] = new LimitedTradeNpc(limitedItems);
            }
        }

        int scheduled = 0;
        foreach (var npc in _limitedTradeNpcs.Values)
        {
            foreach (var limitedItem in npc.LimitedItems)
            {
                if (string.IsNullOrWhiteSpace(limitedItem.SalesTime)) continue;
                if (await _cronService.Schedule(limitedItem.ResetToDefault, limitedItem.SalesTime))
                    scheduled++;
            }
        }

        _log.LogInformation(
            "LimitedItemTradeService: scheduled {Count} limited item restock(s) across {NpcCount} npc(s)",
            scheduled, _limitedTradeNpcs.Count);
    }

    public bool IsLimitedTradeNpc(int npcId) => _limitedTradeNpcs.ContainsKey(npcId);

    public LimitedItem? GetLimitedItem(int npcId, int itemId)
    {
        if (!_limitedTradeNpcs.TryGetValue(npcId, out var npc)) return null;
        foreach (var item in npc.LimitedItems)
        {
            if (item.ItemId == itemId) return item;
        }
        return null;
    }

    /// <summary>Remaining global stock for a limited item, or null if the item is not stock-limited
    /// (unlimited, not sold by this npc, or limited only by a per-player purchase cap).</summary>
    public long? GetRemaining(int npcId, int itemId) => GetLimitedItem(npcId, itemId)?.Remaining;

    /// <summary>
    /// Atomically checks and reserves stock/purchase-cap for a purchase of <paramref name="requestedCount"/>.
    /// Returns the count actually reserved — may be less than requested if only partially in stock, 0 if
    /// sold out / the player's cap is already reached, and simply <paramref name="requestedCount"/>
    /// unchanged if the item isn't limited at all. Callers that abort the purchase after reserving
    /// (insufficient kinah, inventory full, etc.) must call <see cref="RestoreStock"/> with the same count.
    /// </summary>
    public long DecreaseStock(int npcId, int itemId, int playerObjectId, long requestedCount)
        => GetLimitedItem(npcId, itemId)?.TryReserve(playerObjectId, requestedCount) ?? requestedCount;

    /// <summary>Undoes a previous <see cref="DecreaseStock"/> reservation of the same count.</summary>
    public void RestoreStock(int npcId, int itemId, int playerObjectId, long count)
        => GetLimitedItem(npcId, itemId)?.Release(playerObjectId, count);
}
