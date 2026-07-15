using AionLightning.Game.Model.LimitedItems;

namespace AionLightning.Game.Model.Templates.Goods;

/// <summary>
/// Port of Java <c>model.templates.goods.GoodsList</c> — a goodslist template (referenced by npc
/// trade-list tabs) enumerating item ids and, for a subset, per-item sell/buy limits plus a shared cron
/// restock time. Java unmarshalled this via JAXB annotations; here it is populated directly by
/// <see cref="AionLightning.Game.DataHolders.ShopData"/> while it streams goodslists.xml.
/// </summary>
public sealed class GoodsList
{
    public int Id { get; init; }
    public string? SalesTime { get; init; }
    public IReadOnlyList<GoodsListItem> Items { get; init; } = [];

    /// <summary>
    /// Java <c>GoodsList.getLimitedItems()</c> — items carrying both sell_limit and buy_limit
    /// attributes are "limited" (finite stock and/or per-player purchase cap); plain items (neither
    /// attribute present) are excluded.
    /// </summary>
    public IReadOnlyList<LimitedItem> GetLimitedItems()
    {
        var result = new List<LimitedItem>();
        foreach (var item in Items)
        {
            if (item.SellLimit.HasValue && item.BuyLimit.HasValue)
                result.Add(new LimitedItem(item.Id, item.SellLimit.Value, item.BuyLimit.Value, SalesTime));
        }
        return result;
    }
}

/// <summary>Port of Java <c>GoodsList.Item</c> — a single goodslist entry.</summary>
public sealed class GoodsListItem
{
    public int Id { get; init; }
    public int? SellLimit { get; init; }
    public int? BuyLimit { get; init; }
}
