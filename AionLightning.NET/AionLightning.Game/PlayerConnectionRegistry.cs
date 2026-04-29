using System.Collections.Concurrent;
using AionLightning.Game.Network.Aion;

namespace AionLightning.Game;

/// <summary>Maps online player objectId → their active GsClientConnection for broadcast.</summary>
public sealed class PlayerConnectionRegistry
{
    private readonly ConcurrentDictionary<int, GsClientConnection> _map = new();

    public void Register(int objectId, GsClientConnection conn) => _map[objectId] = conn;

    public void Unregister(int objectId) => _map.TryRemove(objectId, out _);

    public GsClientConnection? Get(int objectId) => _map.TryGetValue(objectId, out var conn) ? conn : null;

    public IEnumerable<GsClientConnection> GetAll() => _map.Values;

    public IEnumerable<GsClientConnection> GetAllExcept(int objectId)
        => _map.Where(kv => kv.Key != objectId).Select(kv => kv.Value);
}
