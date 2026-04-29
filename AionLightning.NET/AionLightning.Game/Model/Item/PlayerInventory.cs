namespace AionLightning.Game.Model.Item;

public sealed class PlayerInventory
{
    private readonly Dictionary<long, Item> _items = new();

    public void Add(Item item)           => _items[item.UniqueId] = item;
    public bool Remove(long uniqueId)    => _items.Remove(uniqueId);
    public Item? Get(long uniqueId)      => _items.GetValueOrDefault(uniqueId);

    public IEnumerable<Item> All         => _items.Values;
    public int Count                     => _items.Count;
}
