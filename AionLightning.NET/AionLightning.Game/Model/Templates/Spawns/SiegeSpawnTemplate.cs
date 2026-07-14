using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Model.Templates.Spawns;

/// <summary>
/// Java model.templates.spawns.siegespawns.SiegeSpawnTemplate — one static siege-NPC spawn point,
/// flattened out of the nested <c>data/static_data/spawns/Sieges/*.xml</c> structure
/// (spawn_map/siege_spawn/siege_race/siege_mod/spawn/spot) by
/// <see cref="AionLightning.Game.DataHolders.SiegeSpawnData"/>. Unlike Java's inheritance-based
/// SpawnGroup2/SpawnTemplate/SiegeSpawnTemplate class hierarchy, this port uses a single flat record
/// (the project already applies the same simplification to regular NPC spawns — see
/// <see cref="AionLightning.Game.DataHolders.SpawnEntry"/>/<see cref="AionLightning.Game.DataHolders.SpawnSpot"/>).
/// </summary>
public sealed class SiegeSpawnTemplate
{
    public required int SiegeId { get; init; }
    public required SiegeRace SiegeRace { get; init; }
    public required SiegeModType SiegeModType { get; init; }
    public required int WorldId { get; init; }
    public required int NpcId { get; init; }
    public int RespawnTime { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
    public byte Heading { get; init; }

    /// <summary>Java SpawnTemplate.getStaticId() — non-zero for door/structure-bound siege props (e.g.
    /// the artifact/gate objects tied to a fixed static object id). Not consumed by the spawn engine yet;
    /// kept so a future phase (static-object/door wiring) doesn't need another data pass.</summary>
    public int StaticId { get; init; }
}
