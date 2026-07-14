using System.Xml.Serialization;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Model.Templates.Spawns;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/spawns/Sieges/*.xml — mirrors Java's
// model.templates.spawns.siegespawns.SiegeSpawn (+ nested SiegeRaceTemplate/SiegeModTemplate) JAXB
// binding: <spawns><spawn_map map_id=".."><siege_spawn siege_id=".."><siege_race race=".."><siege_mod
// mod=".."><spawn npc_id=".." respawn_time=".."><spot x=".." y=".." z=".." h=".." static_id=".."/></spawn>
// </siege_mod></siege_race></siege_spawn></spawn_map></spawns>. Kept private to this file (like
// SpawnsData.cs's own SpawnEntry/SpawnSpot) — callers consume the flattened SiegeSpawnTemplate instead.

[XmlRoot("spot")]
public sealed class SiegeSpawnSpotXml
{
    [XmlAttribute("x")]         public float X        { get; set; }
    [XmlAttribute("y")]         public float Y        { get; set; }
    [XmlAttribute("z")]         public float Z        { get; set; }
    [XmlAttribute("h")]         public byte  Heading  { get; set; }
    [XmlAttribute("static_id")] public int   StaticId { get; set; }
}

[XmlRoot("spawn")]
public sealed class SiegeSpawnEntryXml
{
    [XmlAttribute("npc_id")]       public int                       NpcId       { get; set; }
    [XmlAttribute("respawn_time")] public int                       RespawnTime { get; set; }
    [XmlElement("spot")]           public List<SiegeSpawnSpotXml>   Spots       { get; set; } = new();
}

[XmlRoot("siege_mod")]
public sealed class SiegeModXml
{
    [XmlAttribute("mod")] public SiegeModType               Mod    { get; set; }
    [XmlElement("spawn")] public List<SiegeSpawnEntryXml>    Spawns { get; set; } = new();
}

[XmlRoot("siege_race")]
public sealed class SiegeRaceXml
{
    [XmlAttribute("race")]    public SiegeRace         Race { get; set; }
    [XmlElement("siege_mod")] public List<SiegeModXml> Mods { get; set; } = new();
}

[XmlRoot("siege_spawn")]
public sealed class SiegeSpawnGroupXml
{
    [XmlAttribute("siege_id")]  public int                   SiegeId { get; set; }
    [XmlElement("siege_race")]  public List<SiegeRaceXml>    Races   { get; set; } = new();
}

[XmlRoot("spawn_map")]
public sealed class SiegeSpawnMapXml
{
    [XmlAttribute("map_id")]     public int                      MapId       { get; set; }
    [XmlElement("siege_spawn")]  public List<SiegeSpawnGroupXml> SiegeSpawns { get; set; } = new();
}

[XmlRoot("spawns")]
public sealed class SiegeSpawnsFileXml
{
    [XmlElement("spawn_map")] public List<SiegeSpawnMapXml> Maps { get; set; } = new();
}

/// <summary>
/// Java dataholders.SpawnsData2's siege-spawn half (<c>getSiegeSpawnsByLocId</c>) — loads
/// data/static_data/spawns/Sieges/*.xml and flattens the nested spawn_map/siege_spawn/siege_race/
/// siege_mod/spawn/spot structure into a lookup keyed by siege_id, since that's the only key
/// <see cref="AionLightning.Game.Services.SiegeService.SpawnNpcs"/> ever queries by.
/// </summary>
public sealed class SiegeSpawnData
{
    private readonly Dictionary<int, List<SiegeSpawnTemplate>> _bySiegeId = new();
    private static readonly XmlSerializer _fileSerializer = new(typeof(SiegeSpawnsFileXml));

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "spawns", "Sieges");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("SiegeSpawnData: siege spawns directory not found: {Dir}", dir);
            return;
        }

        int total = 0;
        foreach (var file in Directory.GetFiles(dir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (SiegeSpawnsFileXml)_fileSerializer.Deserialize(fs)!;
                foreach (var map in data.Maps)
                foreach (var group in map.SiegeSpawns)
                foreach (var race in group.Races)
                foreach (var mod in race.Mods)
                foreach (var spawn in mod.Spawns)
                foreach (var spot in spawn.Spots)
                {
                    var template = new SiegeSpawnTemplate
                    {
                        SiegeId      = group.SiegeId,
                        SiegeRace    = race.Race,
                        SiegeModType = mod.Mod,
                        WorldId      = map.MapId,
                        NpcId        = spawn.NpcId,
                        RespawnTime  = spawn.RespawnTime,
                        X            = spot.X,
                        Y            = spot.Y,
                        Z            = spot.Z,
                        Heading      = spot.Heading,
                        StaticId     = spot.StaticId,
                    };

                    if (!_bySiegeId.TryGetValue(group.SiegeId, out var list))
                    {
                        list = new List<SiegeSpawnTemplate>();
                        _bySiegeId[group.SiegeId] = list;
                    }
                    list.Add(template);
                    total++;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SiegeSpawnData: failed to parse {File}", Path.GetFileName(file));
            }
        }

        log.LogInformation("SiegeSpawnData: loaded {Total} siege spawn spots across {Sieges} siege ids", total, _bySiegeId.Count);
    }

    public IReadOnlyList<SiegeSpawnTemplate> GetSiegeSpawnsBySiegeId(int siegeId)
        => _bySiegeId.TryGetValue(siegeId, out var list) ? list : Array.Empty<SiegeSpawnTemplate>();
}
