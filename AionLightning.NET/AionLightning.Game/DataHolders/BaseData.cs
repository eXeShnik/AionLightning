using System.Xml.Serialization;
using AionLightning.Game.Model.Base;
using AionLightning.Game.Model.Templates.Base;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

[XmlRoot("base_locations")]
public sealed class BaseLocationsFileXml
{
    [XmlElement("base_location")] public List<BaseTemplate> Locations { get; set; } = [];
}

/// <summary>
/// Loads data/static_data/base/base_locations.xml (Java dataholders.BaseData) into the runtime
/// <see cref="BaseLocation"/> map consumed by <see cref="AionLightning.Game.Services.BaseService"/> —
/// the same load-then-flatten shape as <see cref="SiegeLocationData"/>.
/// </summary>
public sealed class BaseData
{
    private static readonly XmlSerializer Serializer = new(typeof(BaseLocationsFileXml));

    private readonly Dictionary<int, BaseLocation> _locations = new();

    public IReadOnlyDictionary<int, BaseLocation> Locations => _locations;

    public int Count => _locations.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "base", "base_locations.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("BaseData: base_locations.xml not found at {Path}", path);
            return;
        }

        BaseLocationsFileXml? data;
        using (var fs = File.OpenRead(path))
            data = (BaseLocationsFileXml?)Serializer.Deserialize(fs);

        if (data?.Locations is null)
        {
            log.LogWarning("BaseData: base_locations.xml produced no templates");
            return;
        }

        foreach (var template in data.Locations)
            _locations[template.Id] = new BaseLocation(template);

        log.LogInformation("BaseData: loaded {Count} base location(s)", _locations.Count);
    }
}
