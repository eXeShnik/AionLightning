using System.Xml.Serialization;
using AionLightning.Game.Model.Rift;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

[XmlRoot("rift_location")]
public sealed class RiftLocationXml
{
    [XmlAttribute("id")]    public int Id      { get; set; }
    [XmlAttribute("world")] public int WorldId  { get; set; }
}

[XmlRoot("rift_locations")]
public sealed class RiftLocationsFileXml
{
    [XmlElement("rift_location")] public List<RiftLocationXml> Locations { get; set; } = [];
}

/// <summary>
/// Loads data/static_data/rift/rift_locations.xml (Java dataholders.RiftData) into the runtime
/// <see cref="RiftLocation"/> map consumed by RiftService, keyed by location id (matches
/// <see cref="RiftEnum"/> and rift_schedule.xml).
/// </summary>
public sealed class RiftData
{
    private static readonly XmlSerializer Serializer = new(typeof(RiftLocationsFileXml));

    private readonly Dictionary<int, RiftLocation> _locations = new();

    public IReadOnlyDictionary<int, RiftLocation> Locations => _locations;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "rift", "rift_locations.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("RiftData: rift_locations.xml not found at {Path}", path);
            return;
        }

        RiftLocationsFileXml? data;
        using (var fs = File.OpenRead(path))
            data = (RiftLocationsFileXml?)Serializer.Deserialize(fs);

        if (data?.Locations is null)
        {
            log.LogWarning("RiftData: rift_locations.xml produced no templates");
            return;
        }

        foreach (var xml in data.Locations)
            _locations[xml.Id] = new RiftLocation(new RiftTemplate(xml.Id, xml.WorldId));

        log.LogInformation("RiftData: loaded {Count} rift location(s)", _locations.Count);
    }
}
