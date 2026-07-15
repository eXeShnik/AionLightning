using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/spawns/Vortex/*.xml — mirrors Java's
// model.templates.spawns.vortexspawns.{VortexSpawn,VortexSpawnTemplate} JAXB binding:
// <spawns><spawn_map map_id=".."><vortex_spawn id=".."><state_type state="PEACE|INVASION">
// <spawn npc_id=".." respawn_time=".."><spot x=".." y=".." z=".." h=".."/></spawn></state_type>
// </vortex_spawn></spawn_map></spawns>. Kept private to this file, like every other DataHolders XML
// shape in this project.

[XmlRoot("spot")]
public sealed class VortexSpawnSpotXml
{
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("h")] public byte Heading { get; set; }
}

[XmlRoot("spawn")]
public sealed class VortexSpawnEntryXml
{
    [XmlAttribute("npc_id")] public int NpcId { get; set; }
    [XmlAttribute("respawn_time")] public int RespawnTime { get; set; }
    [XmlElement("spot")] public List<VortexSpawnSpotXml> Spots { get; set; } = [];
}

[XmlRoot("state_type")]
public sealed class VortexStateTypeXml
{
    [XmlAttribute("state")] public string State { get; set; } = string.Empty;
    [XmlElement("spawn")] public List<VortexSpawnEntryXml> Spawns { get; set; } = [];
}

[XmlRoot("vortex_spawn")]
public sealed class VortexSpawnGroupXml
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlElement("state_type")] public List<VortexStateTypeXml> States { get; set; } = [];
}

[XmlRoot("spawn_map")]
public sealed class VortexSpawnMapXml
{
    [XmlAttribute("map_id")] public int MapId { get; set; }
    [XmlElement("vortex_spawn")] public List<VortexSpawnGroupXml> VortexSpawns { get; set; } = [];
}

[XmlRoot("spawns")]
public sealed class VortexSpawnsFileXml
{
    [XmlElement("spawn_map")] public List<VortexSpawnMapXml> Maps { get; set; } = [];
}

/// <summary>Java model.vortex.VortexStateType (PEACE/INVASION) — which of a vortex location's two spawn
/// sets a <see cref="VortexSpawnTemplate"/> belongs to.</summary>
public enum VortexSpawnState
{
    Peace,
    Invasion,
}

/// <summary>
/// Java model.templates.spawns.vortexspawns.VortexSpawnTemplate, flattened out of the nested
/// data/static_data/spawns/Vortex/*.xml structure (spawn_map/vortex_spawn/state_type/spawn/spot) by
/// <see cref="VortexSpawnData"/> — the same flattening approach this project already applies to rift/
/// siege/base spawns.
/// </summary>
public sealed record VortexSpawnTemplate(
    int LocationId,
    VortexSpawnState State,
    int WorldId,
    int NpcId,
    int RespawnTime,
    float X,
    float Y,
    float Z,
    byte Heading);

/// <summary>
/// Java dataholders.SpawnsData2's vortex-spawn half (<c>getVortexSpawnsByLocId</c>) — loads
/// data/static_data/spawns/Vortex/*.xml and flattens it into a lookup keyed by vortex_spawn id (the
/// same id as <see cref="Model.Vortex.VortexLocation.Id"/>/dimensional_vortex.xml), the only key
/// <see cref="Services.VortexService"/> ever queries by.
/// note: unlike Bases/Gather/Instances/Npcs/Rifts/Sieges/Statics, this repo's Java reference data set
/// (AL-Game/data/static_data/spawns/) never actually shipped a Vortex/ subdirectory — the Dimensional
/// Vortex invasion's peace/war NPC garrison was left without static data upstream. This loader still
/// implements the real Java XML shape for real (so dropping in an operator-authored file "just works"),
/// but until one exists, <see cref="GetSpawns"/> always returns empty and VortexService's invasion
/// start/stop spawns only the master/slave entry portal pair (whose coordinates come from
/// <see cref="RiftSpawnData"/> instead — see VortexService's doc comment).
/// </summary>
public sealed class VortexSpawnData
{
    private static readonly XmlSerializer Serializer = new(typeof(VortexSpawnsFileXml));

    private readonly Dictionary<int, List<VortexSpawnTemplate>> _byLocationId = new();

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "spawns", "Vortex");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("VortexSpawnData: vortex spawns directory not found: {Dir} — this repo's Java " +
                "reference tree never shipped one either, so invasion peace/war garrison NPCs won't spawn " +
                "until an operator supplies this data (see this class's doc comment)", dir);
            return;
        }

        int total = 0;
        foreach (var file in Directory.GetFiles(dir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (VortexSpawnsFileXml)Serializer.Deserialize(fs)!;
                foreach (var map in data.Maps)
                foreach (var group in map.VortexSpawns)
                foreach (var state in group.States)
                foreach (var spawn in state.Spawns)
                foreach (var spot in spawn.Spots)
                {
                    var template = new VortexSpawnTemplate(
                        group.Id,
                        ParseState(state.State),
                        map.MapId,
                        spawn.NpcId,
                        spawn.RespawnTime,
                        spot.X,
                        spot.Y,
                        spot.Z,
                        spot.Heading);

                    if (!_byLocationId.TryGetValue(group.Id, out var list))
                        _byLocationId[group.Id] = list = [];
                    list.Add(template);
                    total++;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "VortexSpawnData: failed to parse {File}", Path.GetFileName(file));
            }
        }

        log.LogInformation("VortexSpawnData: loaded {Total} vortex spawn spot(s) across {Locations} location id(s)",
            total, _byLocationId.Count);
    }

    public IReadOnlyList<VortexSpawnTemplate> GetSpawns(int locationId, VortexSpawnState state) =>
        _byLocationId.TryGetValue(locationId, out var list)
            ? list.Where(s => s.State == state).ToList()
            : [];

    private static VortexSpawnState ParseState(string state) =>
        string.Equals(state, "INVASION", StringComparison.OrdinalIgnoreCase) ? VortexSpawnState.Invasion : VortexSpawnState.Peace;
}
