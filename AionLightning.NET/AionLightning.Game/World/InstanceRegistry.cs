using System.Collections.Concurrent;

namespace AionLightning.Game.World;

/// <summary>
/// Holds every live instance channel keyed by <c>worldId → instanceId → channel</c> and hands out
/// monotonic per-world instance ids (Java <c>WorldMap.instances</c> + <c>getNextInstanceId</c>).
/// Instance id 0 is reserved for the shared open-world channel and is never allocated here, so
/// channels start at 1.
/// </summary>
public sealed class InstanceRegistry
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, WorldMapInstance>> _byWorld = new();
    private readonly ConcurrentDictionary<int, int> _nextId = new();

    public int NextInstanceId(int worldId)
        => _nextId.AddOrUpdate(worldId, 1, (_, prev) => prev + 1);

    public void Add(WorldMapInstance instance)
    {
        var map = _byWorld.GetOrAdd(instance.WorldId, _ => new ConcurrentDictionary<int, WorldMapInstance>());
        map[instance.InstanceId] = instance;
    }

    public bool Remove(WorldMapInstance instance)
        => _byWorld.TryGetValue(instance.WorldId, out var map) && map.TryRemove(instance.InstanceId, out _);

    public WorldMapInstance? Get(int worldId, int instanceId)
        => _byWorld.TryGetValue(worldId, out var map) && map.TryGetValue(instanceId, out var inst) ? inst : null;

    public bool Exists(int worldId, int instanceId) => Get(worldId, instanceId) is not null;

    public IEnumerable<WorldMapInstance> ByWorld(int worldId)
        => _byWorld.TryGetValue(worldId, out var map) ? map.Values : [];

    public IEnumerable<WorldMapInstance> All()
        => _byWorld.Values.SelectMany(m => m.Values);
}
