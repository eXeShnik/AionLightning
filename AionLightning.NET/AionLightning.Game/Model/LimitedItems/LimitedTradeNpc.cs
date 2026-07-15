namespace AionLightning.Game.Model.LimitedItems;

/// <summary>
/// Port of Java <c>model.limiteditems.LimitedTradeNpc</c> — the set of limited items sold across all of
/// one NPC's trade-list tabs. An NPC's trade list can reference several goodslists; limited items
/// contributed by each are merged into a single per-NPC set here.
/// </summary>
public sealed class LimitedTradeNpc
{
    private readonly List<LimitedItem> _limitedItems;

    public LimitedTradeNpc(IEnumerable<LimitedItem> limitedItems)
    {
        _limitedItems = new List<LimitedItem>(limitedItems);
    }

    public void AddLimitedItems(IEnumerable<LimitedItem> limitedItems) => _limitedItems.AddRange(limitedItems);

    public IReadOnlyList<LimitedItem> LimitedItems => _limitedItems;
}
