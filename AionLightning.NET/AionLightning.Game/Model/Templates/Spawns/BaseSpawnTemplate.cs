using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Model.Templates.Spawns;

/// <summary>
/// Java spawnengine.SpawnHandlerType's base-relevant subset (BOSS/ATTACKER — Java's RIFT/STATIC values
/// are never used by base spawn data). Absent (null, on <see cref="BaseSpawnTemplate.HandlerType"/>)
/// means a regular defender NPC that's simply part of the base's owning-race garrison.
/// </summary>
public enum BaseSpawnHandlerType
{
    Attacker,
    Boss,
}

/// <summary>
/// Java model.templates.spawns.basespawns.BaseSpawnTemplate, flattened out of the nested
/// data/static_data/spawns/Bases/*.xml structure (spawn_map/base_spawn/simple_race/spawn/spot) by
/// <see cref="AionLightning.Game.DataHolders.BaseSpawnData"/> — the same flattening approach this
/// project already applies to siege spawns (see <see cref="SiegeSpawnTemplate"/>).
/// </summary>
public sealed class BaseSpawnTemplate
{
    public required int BaseId { get; init; }
    public required SiegeRace BaseRace { get; init; }
    public required int WorldId { get; init; }
    public required int NpcId { get; init; }
    public int RespawnTime { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
    public byte Heading { get; init; }

    /// <summary>Java's SpawnTemplate/SpawnGroup2 base classes carried the handler type; flattened here
    /// alongside the rest. Null = regular defender (part of the owning race's standing garrison).</summary>
    public BaseSpawnHandlerType? HandlerType { get; init; }
}
