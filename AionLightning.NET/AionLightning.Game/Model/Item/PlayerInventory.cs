namespace AionLightning.Game.Model.Item;

public sealed class PlayerInventory
{
    private readonly Dictionary<long, Item> _items = new();

    public void Add(Item item)           => _items[item.UniqueId] = item;
    public bool Remove(long uniqueId)    => _items.Remove(uniqueId);
    public Item? Get(long uniqueId)      => _items.GetValueOrDefault(uniqueId);
    // By default excludes equipped items — quest turn-ins and crafting consume bag items only.
    // Pass includeEquipped: true only for lookups that intentionally span both (e.g. kinah, exchange).
    public Item? FindByItemId(int itemId, bool includeEquipped = false)
        => _items.Values.FirstOrDefault(i => i.ItemId == itemId && (includeEquipped || !i.IsEquipped));

    public IEnumerable<Item> All         => _items.Values;
    public int Count                     => _items.Count;
}
