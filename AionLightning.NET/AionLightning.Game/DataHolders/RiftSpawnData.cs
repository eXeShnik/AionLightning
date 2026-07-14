using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/spawns/Rifts/*.xml — deliberately separate from
// SpawnsData's SpawnSpot/SpawnEntry/SpawnMapEntry/SpawnsFileXml (same shapes, different files):
// a Rifts/*.xml file mixes a plain anchor-tagged "portal" <spawn> group directly under
// <spawn_map> with one or more <rift_spawn id="..."> guard-spawn groups.

[XmlRoot("spot")]
public sealed class RiftSpotXml
{
    [XmlAttribute("x")]         public float X       { get; set; }
    [XmlAttribute("y")]         public float Y       { get; set; }
    [XmlAttribute("z")]         public float Z       { get; set; }
    [XmlAttribute("h")]         public byte  Heading { get; set; }
    [XmlAttribute("anchor")]    public string Anchor { get; set; } = string.Empty;
}

[XmlRoot("spawn")]
public sealed class RiftSpawnEntryXml
{
    [XmlAttribute("npc_id")]       public int              NpcId       { get; set; }
    [XmlAttribute("respawn_time")] public int              RespawnTime { get; set; }
    [XmlElement("spot")]           public List<RiftSpotXml> Spots      { get; set; } = [];
}

[XmlRoot("rift_spawn")]
public sealed class RiftGuardBlockXml
{
    [XmlAttribute("id")]       public int                     Id     { get; set; }
    [XmlElement("spawn")]      public List<RiftSpawnEntryXml> Spawns { get; set; } = [];
}

[XmlRoot("spawn_map")]
public sealed class RiftSpawnMapXml
{
    [XmlAttribute("map_id")]      public int                       MapId      { get; set; }
    [XmlElement("spawn")]         public List<RiftSpawnEntryXml>   Portals    { get; set; } = [];
    [XmlElement("rift_spawn")]    public List<RiftGuardBlockXml>   RiftSpawns { get; set; } = [];
}

[XmlRoot("spawns")]
public sealed class RiftSpawnsFileXml
{
    [XmlElement("spawn_map")] public List<RiftSpawnMapXml> Maps { get; set; } = [];
}

/// <summary>Master/slave rift portal anchor coordinates (Java's RiftManager.riftGroups, keyed by
/// the &lt;spot anchor="..."/&gt; name that matches <see cref="AionLightning.Game.Model.Rift.RiftEnum"/>'s
/// Master/Slave fields).</summary>
public readonly record struct RiftAnchorPoint(int NpcId, int WorldId, float X, float Y, float Z, byte Heading);

/// <summary>One static guard/defense NPC spawned alongside a rift when its schedule entry's
/// spawn="true" flag is set (Java's DataManager.SPAWNS_DATA2.getRiftSpawnsByLocId(locationId)).</summary>
public readonly record struct RiftGuardPoint(int NpcId, int WorldId, int RespawnTime, float X, float Y, float Z, byte Heading);

/// <summary>
/// Loads data/static_data/spawns/Rifts/*.xml (Java's SpawnsData2 subset covering RiftSpawnTemplate/
/// RiftSpawn) into a flat anchor-name -> portal-coordinate lookup plus a rift-id -> guard-spawn-point
/// lookup, mirroring the same flattening SpawnsData/SiegeSpawnData already apply to their own
/// nested spawn XML shapes.
/// </summary>
public sealed class RiftSpawnData
{
    private static readonly XmlSerializer Serializer = new(typeof(RiftSpawnsFileXml));

    private readonly Dictionary<string, RiftAnchorPoint> _anchors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<RiftGuardPoint>> _guardsByRiftId = new();

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "spawns", "Rifts");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("RiftSpawnData: Rifts spawn directory not found: {Dir}", dir);
            return;
        }

        int guardCount = 0;
        foreach (var file in Directory.GetFiles(dir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (RiftSpawnsFileXml)Serializer.Deserialize(fs)!;
                foreach (var map in data.Maps)
                {
                    foreach (var portalSpawn in map.Portals)
                    foreach (var spot in portalSpawn.Spots)
                    {
                        if (string.IsNullOrEmpty(spot.Anchor)) continue;
                        _anchors[spot.Anchor] = new RiftAnchorPoint(portalSpawn.NpcId, map.MapId, spot.X, spot.Y, spot.Z, spot.Heading);
                    }

                    foreach (var block in map.RiftSpawns)
                    {
                        if (block.Spawns.Count == 0) continue;
                        if (!_guardsByRiftId.TryGetValue(block.Id, out var list))
                            _guardsByRiftId[block.Id] = list = [];

                        foreach (var guardSpawn in block.Spawns)
                        foreach (var spot in guardSpawn.Spots)
                        {
                            list.Add(new RiftGuardPoint(guardSpawn.NpcId, map.MapId, guardSpawn.RespawnTime, spot.X, spot.Y, spot.Z, spot.Heading));
                            guardCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "RiftSpawnData: failed to parse {File}", Path.GetFileName(file));
            }
        }

        log.LogInformation("RiftSpawnData: loaded {Anchors} portal anchor(s), {Guards} guard spawn point(s) across {RiftIds} rift id(s)",
            _anchors.Count, guardCount, _guardsByRiftId.Count);
    }

    public RiftAnchorPoint? GetAnchor(string anchor) => _anchors.TryGetValue(anchor, out var p) ? p : null;

    public IReadOnlyList<RiftGuardPoint> GetGuards(int riftId) =>
        _guardsByRiftId.TryGetValue(riftId, out var list) ? list : [];
}
