using System.Collections.Concurrent;
using WorldMapInstanceType = AionLightning.Game.World.WorldMapInstance;

namespace AionLightning.Game.Instance;

/// <summary>The no-op fallback handler for maps without a dedicated script (Java <c>DUMMY_INSTANCE_HANDLER</c>).</summary>
public sealed class DummyInstanceHandler : GeneralInstanceHandler { }

/// <summary>
/// Registry of instance-handler factories keyed by worldId (Java <c>InstanceEngine</c>). Scripts are
/// discovered and registered by <c>InstanceEngineHostedService</c>; <see cref="GetNewInstanceHandler"/>
/// returns a fresh handler per channel (one instance per channel, never shared — mirrors Java's
/// <c>newInstance()</c>) or a <see cref="DummyInstanceHandler"/> when no script is bound.
/// </summary>
public sealed class InstanceEngine
{
    private readonly ConcurrentDictionary<int, Func<IInstanceHandler>> _factories = new();

    public void Register(int worldId, Func<IInstanceHandler> factory) => _factories[worldId] = factory;

    public bool HasHandler(int worldId) => _factories.ContainsKey(worldId);

    public int RegisteredCount => _factories.Count;

    public IInstanceHandler GetNewInstanceHandler(int worldId)
        => _factories.TryGetValue(worldId, out var factory) ? factory() : new DummyInstanceHandler();

    /// <summary>Binds a freshly-created channel to its handler and fires its create hook.</summary>
    public void OnInstanceCreate(WorldMapInstanceType instance)
    {
        if (instance.Handler is GeneralInstanceHandler g) g.Bind(instance);
        instance.Handler.OnInstanceCreate(instance);
    }
}
