using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types — only what we need for basic static spawns

[XmlRoot("spot")]
public sealed class SpawnSpot
{
    [XmlAttribute("x")]          public float  X        { get; set; }
    [XmlAttribute("y")]          public float  Y        { get; set; }
    [XmlAttribute("z")]          public float  Z        { get; set; }
    [XmlAttribute("h")]          public byte   Heading  { get; set; }
    [XmlAttribute("walker_id")]  public string WalkerId { get; set; } = string.Empty;
}

[XmlRoot("spawn")]
public sealed class SpawnEntry
{
    [XmlAttribute("npc_id")]       public int            NpcId       { get; set; }
    [XmlAttribute("respawn_time")] public int            RespawnTime { get; set; }
    [XmlElement("spot")]           public List<SpawnSpot> Spots      { get; set; } = new();
}

[XmlRoot("spawn_map")]
public sealed class SpawnMapEntry
{
    [XmlAttribute("map_id")] public int             MapId  { get; set; }
    [XmlElement("spawn")]    public List<SpawnEntry> Spawns { get; set; } = new();
}

[XmlRoot("spawns")]
public sealed class SpawnsFileXml
{
    [XmlElement("spawn_map")] public List<SpawnMapEntry> Maps { get; set; } = new();
}

public sealed class SpawnsData
{
    private readonly Dictionary<int, List<SpawnEntry>> _byWorld        = new();
    private readonly Dictionary<int, List<SpawnEntry>> _gatherByWorld  = new();
    private static readonly XmlSerializer _fileSerializer = new(typeof(SpawnsFileXml));

    public void Load(string dataRoot, ILogger log)
    {
        LoadDirectory(Path.Combine(dataRoot, "spawns", "Npcs"), _byWorld, log, "NPC spawns");
        LoadDirectory(Path.Combine(dataRoot, "spawns", "Gather"), _gatherByWorld, log, "gather spawns");
    }

    private static void LoadDirectory(string dir, Dictionary<int, List<SpawnEntry>> target, ILogger log, string label)
    {
        if (!Directory.Exists(dir))
        {
            log.LogWarning("SpawnsData: {Label} directory not found: {Dir}", label, dir);
            return;
        }

        int total = 0;
        foreach (var file in Directory.GetFiles(dir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (SpawnsFileXml)_fileSerializer.Deserialize(fs)!;
                foreach (var map in data.Maps)
                {
                    if (!target.TryGetValue(map.MapId, out var list))
                    {
                        list = new List<SpawnEntry>();
                        target[map.MapId] = list;
                    }
                    list.AddRange(map.Spawns);
                    total += map.Spawns.Count;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SpawnsData: failed to parse {File}", Path.GetFileName(file));
            }
        }

        log.LogInformation("SpawnsData: loaded {Total} {Label} across {Maps} maps", total, label, target.Count);
    }

    public IEnumerable<(int MapId, SpawnEntry Entry)> All()
    {
        foreach (var (mapId, entries) in _byWorld)
            foreach (var e in entries)
                yield return (mapId, e);
    }

    public IEnumerable<(int MapId, SpawnEntry Entry)> AllGather()
    {
        foreach (var (mapId, entries) in _gatherByWorld)
            foreach (var e in entries)
                yield return (mapId, e);
    }

    public IReadOnlyList<SpawnEntry> GetByMap(int mapId)
        => _byWorld.TryGetValue(mapId, out var list) ? list : Array.Empty<SpawnEntry>();

    /// <summary>Returns the first spawn entry and its map for <paramref name="npcId"/>, or null if not found.</summary>
    public (int MapId, SpawnEntry Entry, SpawnSpot Spot)? GetFirstSpawnByNpcId(int npcId)
    {
        foreach (var (mapId, entries) in _byWorld)
            foreach (var entry in entries)
                if (entry.NpcId == npcId && entry.Spots.Count > 0)
                    return (mapId, entry, entry.Spots[0]);
        return null;
    }

    public int MapCount => _byWorld.Count;
}
