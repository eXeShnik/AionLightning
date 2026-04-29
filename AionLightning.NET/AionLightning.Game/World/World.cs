using System.Collections.Concurrent;
using AionLightning.Game.Model;

namespace AionLightning.Game.World;

/// <summary>
/// In-memory registry of all online players. Thread-safe.
/// </summary>
public sealed class World
{
    private readonly ConcurrentDictionary<int, Player> _byObjectId = new();
    private readonly ConcurrentDictionary<string, Player> _byName = new(StringComparer.OrdinalIgnoreCase);

    public bool Add(Player player)
    {
        if (!_byObjectId.TryAdd(player.ObjectId, player)) return false;
        _byName[player.Name] = player;
        return true;
    }

    public bool Remove(Player player)
    {
        _byName.TryRemove(player.Name, out _);
        return _byObjectId.TryRemove(player.ObjectId, out _);
    }

    public Player? GetByObjectId(int objectId)
        => _byObjectId.GetValueOrDefault(objectId);

    public Player? GetByName(string name)
        => _byName.GetValueOrDefault(name);

    public IEnumerable<Player> GetAll() => _byObjectId.Values;

    public int OnlineCount => _byObjectId.Count;
}
