using System.Xml.Serialization;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Model.Templates.Spawns;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/spawns/Bases/*.xml — mirrors Java's
// model.templates.spawns.basespawns.BaseSpawn (+ nested SimpleRaceTemplate) JAXB binding:
// <spawns><spawn_map map_id=".."><base_spawn id=".."><simple_race race=".."><spawn npc_id=".."
// handler=".." respawn_time=".."><spot x=".." y=".." z=".." h=".."/></spawn></simple_race></base_spawn>
// </spawn_map></spawns>. Kept private to this file (like SiegeSpawnData.cs's own binding types) — callers
// consume the flattened BaseSpawnTemplate instead. `race`/`handler` are read as plain strings rather than
// bound directly to SiegeRace/BaseSpawnHandlerType, since the XML's "NPC" race value and the
// handler-less "regular defender" case have no matching enum member on those two flattened types.

[XmlRoot("spot")]
public sealed class BaseSpawnSpotXml
{
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("h")] public byte Heading { get; set; }
}

[XmlRoot("spawn")]
public sealed class BaseSpawnEntryXml
{
    [XmlAttribute("npc_id")] public int NpcId { get; set; }
    [XmlAttribute("handler")] public string? Handler { get; set; }
    [XmlAttribute("respawn_time")] public int RespawnTime { get; set; }
    [XmlElement("spot")] public List<BaseSpawnSpotXml> Spots { get; set; } = new();
}

[XmlRoot("simple_race")]
public sealed class BaseSimpleRaceXml
{
    [XmlAttribute("race")] public string Race { get; set; } = string.Empty;
    [XmlElement("spawn")] public List<BaseSpawnEntryXml> Spawns { get; set; } = new();
}

[XmlRoot("base_spawn")]
public sealed class BaseSpawnGroupXml
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlElement("simple_race")] public List<BaseSimpleRaceXml> Races { get; set; } = new();
}

[XmlRoot("spawn_map")]
public sealed class BaseSpawnMapXml
{
    [XmlAttribute("map_id")] public int MapId { get; set; }
    [XmlElement("base_spawn")] public List<BaseSpawnGroupXml> BaseSpawns { get; set; } = new();
}

[XmlRoot("spawns")]
public sealed class BaseSpawnsFileXml
{
    [XmlElement("spawn_map")] public List<BaseSpawnMapXml> Maps { get; set; } = new();
}

/// <summary>
/// Java dataholders.SpawnsData2's base-spawn half. Java's own services.base.Base.getBaseSpawns() calls
/// <c>DataManager.SPAWNS_DATA2.getBaseSpawnsByLocId(id)</c>, but that method does not exist anywhere on
/// the real SpawnsData2 source in this repository's Java reference tree — the base-capture feature was
/// left unfinished upstream. This port supplies the missing implementation for real: loads
/// data/static_data/spawns/Bases/*.xml and flattens the nested spawn_map/base_spawn/simple_race/spawn/spot
/// structure into a lookup keyed by base_spawn id, the only key
/// <see cref="AionLightning.Game.Services.BaseService"/> ever queries by.
/// </summary>
public sealed class BaseSpawnData
{
    private readonly Dictionary<int, List<BaseSpawnTemplate>> _byBaseId = new();
    private static readonly XmlSerializer FileSerializer = new(typeof(BaseSpawnsFileXml));

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "spawns", "Bases");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("BaseSpawnData: base spawns directory not found: {Dir}", dir);
            return;
        }

        int total = 0;
        foreach (var file in Directory.GetFiles(dir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (BaseSpawnsFileXml)FileSerializer.Deserialize(fs)!;
                foreach (var map in data.Maps)
                foreach (var group in map.BaseSpawns)
                foreach (var race in group.Races)
                foreach (var spawn in race.Spawns)
                foreach (var spot in spawn.Spots)
                {
                    var template = new BaseSpawnTemplate
                    {
                        BaseId      = group.Id,
                        BaseRace    = ParseRace(race.Race),
                        WorldId     = map.MapId,
                        NpcId       = spawn.NpcId,
                        RespawnTime = spawn.RespawnTime,
                        X           = spot.X,
                        Y           = spot.Y,
                        Z           = spot.Z,
                        Heading     = spot.Heading,
                        HandlerType = ParseHandler(spawn.Handler),
                    };

                    if (!_byBaseId.TryGetValue(group.Id, out var list))
                    {
                        list = new List<BaseSpawnTemplate>();
                        _byBaseId[group.Id] = list;
                    }
                    list.Add(template);
                    total++;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "BaseSpawnData: failed to parse {File}", Path.GetFileName(file));
            }
        }

        log.LogInformation("BaseSpawnData: loaded {Total} base spawn spots across {Bases} base ids", total, _byBaseId.Count);
    }

    public IReadOnlyList<BaseSpawnTemplate> GetBaseSpawnsByBaseId(int baseId)
        => _byBaseId.TryGetValue(baseId, out var list) ? list : Array.Empty<BaseSpawnTemplate>();

    /// <summary>Java model.Race's ELYOS/ASMODIANS/NPC (the base spawn XML's race attribute uses
    /// player-race values plus the neutral "NPC" Lepharist marker) mapped onto this port's
    /// <see cref="SiegeRace"/> ownership convention — see BaseLocation's doc comment.</summary>
    private static SiegeRace ParseRace(string race) => race switch
    {
        "ELYOS" => SiegeRace.ELYOS,
        "ASMODIANS" => SiegeRace.ASMODIANS,
        _ => SiegeRace.BALAUR,
    };

    private static BaseSpawnHandlerType? ParseHandler(string? handler) => handler switch
    {
        "BOSS" => BaseSpawnHandlerType.Boss,
        "ATTACKER" => BaseSpawnHandlerType.Attacker,
        _ => null,
    };
}
