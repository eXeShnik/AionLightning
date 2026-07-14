using System.Xml.Serialization;
using AionLightning.Game.Model.World;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/weather_table.xml (Java dataholders.MapWeatherData +
// model.templates.world.WeatherTable/WeatherEntry port).

[XmlRoot("weather")]
public sealed class WeatherFileXml
{
    [XmlElement("map")] public List<WeatherTableXml> Maps { get; set; } = new();
}

public sealed class WeatherTableXml
{
    [XmlAttribute("id")] public int MapId { get; set; }
    [XmlAttribute("zone_count")] public int ZoneCount { get; set; }
    [XmlAttribute("weather_count")] public int WeatherCount { get; set; }
    [XmlElement("table")] public List<WeatherEntryXml> Table { get; set; } = new();
}

public sealed class WeatherEntryXml
{
    [XmlAttribute("zone_id")] public int ZoneId { get; set; }
    [XmlAttribute("code")] public int Code { get; set; }
    [XmlAttribute("rank")] public int Rank { get; set; }
    [XmlAttribute("name")] public string? Name { get; set; }
    [XmlAttribute("before")] public bool Before { get; set; }
    [XmlAttribute("after")] public bool After { get; set; }
}

/// <summary>Java model.templates.world.WeatherTable — the set of possible weather states for a single
/// map, grouped by zone index (1..ZoneCount).</summary>
public sealed class WeatherTable
{
    public required int MapId { get; init; }
    public required int ZoneCount { get; init; }
    public IReadOnlyList<WeatherEntry> ZoneData { get; init; } = [];

    public IEnumerable<WeatherEntry> GetWeathersForZone(int zoneId) => ZoneData.Where(e => e.ZoneId == zoneId);

    /// <summary>Java WeatherTable.getWeatherAfter — finds the "next phase" entry sharing the same
    /// weather name (before -> mid -> after), or null if <paramref name="entry"/> has no successor.</summary>
    public WeatherEntry? GetWeatherAfter(WeatherEntry entry)
    {
        if (entry.Name is null || entry.After) return null;
        foreach (var we in ZoneData)
        {
            if (we.ZoneId != entry.ZoneId || we.Name != entry.Name) continue;
            if (entry.Before && !we.Before && !we.After) return we;
            if (!entry.Before && !entry.After && we.After) return we;
        }
        return null;
    }
}

/// <summary>Java dataholders.MapWeatherData — loads weather_table.xml into per-map <see cref="WeatherTable"/>s.</summary>
public sealed class WeatherData
{
    private readonly Dictionary<int, WeatherTable> _byMapId = new();
    private static readonly XmlSerializer _serializer = new(typeof(WeatherFileXml));

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "weather_table.xml");
        if (!File.Exists(path)) { log.LogWarning("WeatherData: weather_table.xml not found at {P}", path); return; }

        using var fs = File.OpenRead(path);
        if (_serializer.Deserialize(fs) is not WeatherFileXml file) return;

        foreach (var map in file.Maps)
        {
            _byMapId[map.MapId] = new WeatherTable
            {
                MapId = map.MapId,
                ZoneCount = map.ZoneCount,
                ZoneData = map.Table.Select(t => new WeatherEntry(t.ZoneId, t.Code, t.Rank, t.Name, t.Before, t.After)).ToList(),
            };
        }

        log.LogInformation("WeatherData: loaded {Count} map weather tables", _byMapId.Count);
    }

    public WeatherTable? GetWeather(int mapId) => _byMapId.GetValueOrDefault(mapId);

    public IEnumerable<int> MapIds => _byMapId.Keys;
}
