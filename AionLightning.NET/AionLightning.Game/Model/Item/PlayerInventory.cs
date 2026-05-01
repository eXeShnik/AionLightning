namespace AionLightning.Game.Model.Item;

public sealed class PlayerInventory
{
    // Default cube capacity: 27 bag slots (excludes equipped items).
    // Can grow via cube-extension items, not yet implemented.
    public int Capacity { get; set; } = 27;

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

    /// <summary>
    /// Returns the number of unequipped (bag) items. Each unique stack occupies one slot.
    /// </summary>
    public int BagSlotUsed => _items.Values.Count(i => !i.IsEquipped);

    /// <summary>
    /// Returns true when at least one bag slot is free for a new, non-stackable item entry.
    /// </summary>
    public bool HasFreeSlot => BagSlotUsed < Capacity;

    /// <summary>
    /// Returns true when the item can be received without occupying a new slot
    /// (it will stack onto an existing entry) OR when a free bag slot is available.
    /// </summary>
    public bool CanReceive(int itemId, int maxStackCount)
    {
        var existing = FindByItemId(itemId);
        if (existing is not null && maxStackCount > 1) return true; // stacks — no new slot needed
        return HasFreeSlot;
    }
}
