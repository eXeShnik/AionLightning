using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types — only what we need for basic static spawns

[XmlRoot("spot")]
public sealed class SpawnSpot
{
    [XmlAttribute("x")] public float X       { get; set; }
    [XmlAttribute("y")] public float Y       { get; set; }
    [XmlAttribute("z")] public float Z       { get; set; }
    [XmlAttribute("h")] public byte  Heading { get; set; }
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
internal sealed class SpawnsFileXml
{
    [XmlElement("spawn_map")] public List<SpawnMapEntry> Maps { get; set; } = new();
}

public sealed class SpawnsData
{
    private readonly Dictionary<int, List<SpawnEntry>> _byWorld = new();
    private static readonly XmlSerializer _fileSerializer = new(typeof(SpawnsFileXml));

    public void Load(string dataRoot, ILogger log)
    {
        var npcsDir = Path.Combine(dataRoot, "spawns", "Npcs");
        if (!Directory.Exists(npcsDir))
        {
            log.LogWarning("SpawnsData: directory not found: {Dir}", npcsDir);
            return;
        }

        int total = 0;
        foreach (var file in Directory.GetFiles(npcsDir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (SpawnsFileXml)_fileSerializer.Deserialize(fs)!;
                foreach (var map in data.Maps)
                {
                    if (!_byWorld.TryGetValue(map.MapId, out var list))
                    {
                        list = new List<SpawnEntry>();
                        _byWorld[map.MapId] = list;
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

        log.LogInformation("SpawnsData: loaded {Total} spawns across {Maps} maps", total, _byWorld.Count);
    }

    public IEnumerable<(int MapId, SpawnEntry Entry)> All()
    {
        foreach (var (mapId, entries) in _byWorld)
            foreach (var e in entries)
                yield return (mapId, e);
    }

    public IReadOnlyList<SpawnEntry> GetByMap(int mapId)
        => _byWorld.TryGetValue(mapId, out var list) ? list : Array.Empty<SpawnEntry>();

    public int MapCount => _byWorld.Count;
}
