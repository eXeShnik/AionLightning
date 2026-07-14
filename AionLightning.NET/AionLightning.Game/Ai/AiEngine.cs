using System.Collections.Concurrent;
using AionLightning.Game.Model;

namespace AionLightning.Game.Ai;

/// <summary>
/// Registry of per-NPC AI factories keyed by ai-name (Java <c>AI2Engine</c>/<c>AiNames</c>).
/// Scripts are discovered and registered by <see cref="AiEngineHostedService"/>; <see cref="Create"/>
/// hands the spawner a fresh <see cref="NpcAi2"/> bound to its owner, or null when no script is
/// registered for that name (analog of <see cref="Instance.InstanceEngine"/>'s
/// <c>DummyInstanceHandler</c> fallback, except here the caller keeps using the archetype-driven
/// <see cref="Services.NpcAiService"/> tick when there's no script).
/// </summary>
public sealed class AiEngine
{
    private readonly ConcurrentDictionary<string, Func<NpcAi2>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string aiName, Func<NpcAi2> factory) => _factories[aiName] = factory;

    public bool HasAi(string aiName) => !string.IsNullOrEmpty(aiName) && _factories.ContainsKey(aiName);

    public int RegisteredCount => _factories.Count;

    /// <summary>Creates a fresh AI instance for <paramref name="owner"/>, or null if no script is registered for <paramref name="aiName"/>.</summary>
    public NpcAi2? Create(string aiName, Npc owner)
    {
        if (!_factories.TryGetValue(aiName, out var factory)) return null;
        var ai = factory();
        ai.Owner = owner;
        return ai;
    }
}
