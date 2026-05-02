namespace AionLightning.Game.Services;

/// <summary>
/// Session-scoped buy-back store. When a player sells items to an NPC shop, those items
/// are held here (keyed by player ObjectId) so the player can repurchase them later.
/// Items are not persisted — the list is lost on server restart or player logout.
/// </summary>
public sealed class RepurchaseService
{
    public sealed record RepurchaseEntry(long UniqueId, int ItemId, long Count, long RepurchasePrice);

    private readonly Dictionary<int, List<RepurchaseEntry>> _items = new();
    private readonly object _lock = new();

    public void Add(int playerObjectId, IEnumerable<RepurchaseEntry> entries)
    {
        lock (_lock)
        {
            if (!_items.TryGetValue(playerObjectId, out var list))
            {
                list = [];
                _items[playerObjectId] = list;
            }
            list.AddRange(entries);
        }
    }

    public void Remove(int playerObjectId, long uniqueId)
    {
        lock (_lock)
        {
            if (_items.TryGetValue(playerObjectId, out var list))
                list.RemoveAll(e => e.UniqueId == uniqueId);
        }
    }

    public void Clear(int playerObjectId)
    {
        lock (_lock)
            _items.Remove(playerObjectId);
    }

    public IReadOnlyList<RepurchaseEntry> GetAll(int playerObjectId)
    {
        lock (_lock)
            return _items.TryGetValue(playerObjectId, out var list) ? list.ToList() : [];
    }

    public RepurchaseEntry? Get(int playerObjectId, long uniqueId)
    {
        lock (_lock)
        {
            if (!_items.TryGetValue(playerObjectId, out var list)) return null;
            return list.FirstOrDefault(e => e.UniqueId == uniqueId);
        }
    }
}
