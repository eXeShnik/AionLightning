using System.Xml.Serialization;
using AionLightning.Game.Model.Vortex;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/vortex/dimensional_vortex.xml (Java's
// model.templates.vortex.VortexTemplate JAXB binding) — kept private to this file, like every other
// DataHolders XML shape in this project; callers consume the flattened Model.Vortex.VortexTemplate/
// VortexLocation instead.

[XmlRoot("home_point")]
public sealed class VortexHomePointXml
{
    [XmlAttribute("map")] public int Map { get; set; }
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("h")] public byte Heading { get; set; }
}

[XmlRoot("resurrection_point")]
public sealed class VortexResurrectionPointXml
{
    [XmlAttribute("map")] public int Map { get; set; }
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("h")] public byte Heading { get; set; }
}

[XmlRoot("start_point")]
public sealed class VortexStartPointXml
{
    [XmlAttribute("map")] public int Map { get; set; }
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("h")] public byte Heading { get; set; }
}

[XmlRoot("vortex_location")]
public sealed class VortexLocationXml
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlAttribute("defends_race")] public string DefendsRace { get; set; } = string.Empty;
    [XmlAttribute("offence_race")] public string OffenceRace { get; set; } = string.Empty;
    [XmlElement("home_point")] public VortexHomePointXml? Home { get; set; }
    [XmlElement("resurrection_point")] public VortexResurrectionPointXml? Resurrection { get; set; }
    [XmlElement("start_point")] public VortexStartPointXml? Start { get; set; }
}

[XmlRoot("dimensional_vortex")]
public sealed class VortexLocationsFileXml
{
    [XmlElement("vortex_location")] public List<VortexLocationXml> Locations { get; set; } = [];
}

/// <summary>
/// Loads data/static_data/vortex/dimensional_vortex.xml (Java dataholders.VortexData) into the runtime
/// <see cref="VortexLocation"/> map consumed by <see cref="Services.VortexService"/>. Ships in this
/// repo's data set with exactly the two Java-hardcoded locations (id 0 = Theobomos/Marchutan, id 1 =
/// Brusthonin/Kaisinel).
/// </summary>
public sealed class VortexData
{
    private static readonly XmlSerializer Serializer = new(typeof(VortexLocationsFileXml));

    private readonly Dictionary<int, VortexLocation> _locations = new();

    public IReadOnlyDictionary<int, VortexLocation> Locations => _locations;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "vortex", "dimensional_vortex.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("VortexData: dimensional_vortex.xml not found at {Path}", path);
            return;
        }

        VortexLocationsFileXml? data;
        using (var fs = File.OpenRead(path))
            data = (VortexLocationsFileXml?)Serializer.Deserialize(fs);

        if (data?.Locations is null)
        {
            log.LogWarning("VortexData: dimensional_vortex.xml produced no templates");
            return;
        }

        foreach (var xml in data.Locations)
        {
            if (xml.Home is null || xml.Resurrection is null || xml.Start is null)
            {
                log.LogWarning("VortexData: vortex_location {Id} is missing a home/resurrection/start point, skipping", xml.Id);
                continue;
            }

            var template = new VortexTemplate(
                xml.Id,
                ParseRace(xml.DefendsRace),
                ParseRace(xml.OffenceRace),
                new VortexPoint(xml.Home.Map, xml.Home.X, xml.Home.Y, xml.Home.Z, xml.Home.Heading),
                new VortexPoint(xml.Resurrection.Map, xml.Resurrection.X, xml.Resurrection.Y, xml.Resurrection.Z, xml.Resurrection.Heading),
                new VortexPoint(xml.Start.Map, xml.Start.X, xml.Start.Y, xml.Start.Z, xml.Start.Heading));

            _locations[xml.Id] = new VortexLocation(template);
        }

        log.LogInformation("VortexData: loaded {Count} vortex location(s)", _locations.Count);
    }

    /// <summary>dimensional_vortex.xml only ever uses ELYOS/ASMODIANS for defends_race/offence_race —
    /// mirrors BaseSpawnData.ParseRace's defensive fallback shape rather than throwing on unexpected data.</summary>
    private static Model.Race ParseRace(string race) => race switch
    {
        "ELYOS" => Model.Race.ELYOS,
        "ASMODIANS" => Model.Race.ASMODIANS,
        _ => Model.Race.PC_ALL,
    };
}
