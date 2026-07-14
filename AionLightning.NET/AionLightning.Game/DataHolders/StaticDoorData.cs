using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.StaticDoor;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types — the file groups doors per world (Java StaticDoorWorld / StaticDoorData).

[XmlRoot("world")]
public sealed class StaticDoorWorldXml
{
    [XmlAttribute("world")]    public int World { get; set; }
    [XmlElement("staticdoor")] public List<StaticDoorTemplate> Doors { get; set; } = new();
}

[XmlRoot("staticdoor_templates")]
public sealed class StaticDoorTemplatesXml
{
    [XmlElement("world")] public List<StaticDoorWorldXml> Worlds { get; set; } = new();
}

/// <summary>Loads static_data/staticdoors/staticdoor_templates.xml (Java dataholders.StaticDoorData).</summary>
public sealed class StaticDoorData
{
    private readonly Dictionary<int, List<StaticDoorTemplate>> _byWorld = new();
    private static readonly XmlSerializer _serializer = new(typeof(StaticDoorTemplatesXml));

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "staticdoors", "staticdoor_templates.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("StaticDoorData: file not found: {Path}", path);
            return;
        }

        try
        {
            using var fs = File.OpenRead(path);
            var data = (StaticDoorTemplatesXml)_serializer.Deserialize(fs)!;
            int total = 0;
            foreach (var world in data.Worlds)
            {
                _byWorld[world.World] = world.Doors;
                total += world.Doors.Count;
            }
            log.LogInformation("StaticDoorData: loaded {Total} static doors across {Worlds} worlds", total, _byWorld.Count);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "StaticDoorData: failed to parse {Path}", path);
        }
    }

    /// <summary>Java StaticDoorData.getStaticDoorWorlds(world).getStaticDoors() — every door template
    /// placed on <paramref name="worldId"/>'s map, or empty when the map has none.</summary>
    public IReadOnlyList<StaticDoorTemplate> GetWorldDoors(int worldId)
        => _byWorld.TryGetValue(worldId, out var list) ? list : Array.Empty<StaticDoorTemplate>();

    public IEnumerable<int> WorldIds => _byWorld.Keys;
    public int WorldCount => _byWorld.Count;
}
