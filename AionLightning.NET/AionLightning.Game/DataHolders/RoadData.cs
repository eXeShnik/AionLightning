using System.Xml.Serialization;
using AionLightning.Game.Model.Road;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/roads/roads.xml (schema: roads/roads.xsd) — mirrors Java's
// model.templates.road.{RoadTemplate,RoadPoint,RoadExit} JAXB binding: <roads><road name=".." map=".."
// radius=".."><center x=".." y=".." z=".."/><p1 .../><p2 .../><roadexit mapid=".." x=".." y=".." z=".."/>
// </road></roads>. Kept private to this file (like SiegeSpawnData.cs's own XML types) — callers consume
// the flattened RoadTemplate instead.

// Reused under three different element names (center/p1/p2) via [XmlElement] on RoadXml below — no
// single [XmlRoot] name would be accurate, so none is declared here.
public sealed class RoadPointXml
{
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
}

[XmlRoot("roadexit")]
public sealed class RoadExitXml
{
    [XmlAttribute("mapid")] public int MapId { get; set; }
    [XmlAttribute("x")]     public float X    { get; set; }
    [XmlAttribute("y")]     public float Y    { get; set; }
    [XmlAttribute("z")]     public float Z    { get; set; }
}

[XmlRoot("road")]
public sealed class RoadXml
{
    [XmlAttribute("name")]     public string?     Name     { get; set; }
    [XmlAttribute("map")]      public int          Map      { get; set; }
    [XmlAttribute("radius")]   public float         Radius   { get; set; }
    [XmlElement("center")]     public RoadPointXml? Center   { get; set; }
    [XmlElement("p1")]         public RoadPointXml? P1       { get; set; }
    [XmlElement("p2")]         public RoadPointXml? P2       { get; set; }
    [XmlElement("roadexit")]   public RoadExitXml?  RoadExit { get; set; }
}

[XmlRoot("roads")]
public sealed class RoadsFileXml
{
    [XmlElement("road")] public List<RoadXml> Roads { get; set; } = new();
}

/// <summary>
/// Java dataholders.RoadData — loads data/static_data/roads/roads.xml into the flattened
/// <see cref="RoadTemplate"/> list <see cref="AionLightning.Game.Services.RoadService"/> consumes. Java
/// assigned no id to a road (it was only ever referenced by object reference, ping-ponged straight from
/// RoadData into RoadService's constructor loop); this port assigns a stable sequential <see cref="RoadTemplate.Id"/>
/// in file/document order so RoadService can expose a lookup-by-id API.
/// </summary>
public sealed class RoadData
{
    private static readonly XmlSerializer FileSerializer = new(typeof(RoadsFileXml));

    private readonly List<RoadTemplate> _templates = new();

    public IReadOnlyList<RoadTemplate> Templates => _templates;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "roads", "roads.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("RoadData: roads.xml not found at {Path}", path);
            return;
        }

        RoadsFileXml? data;
        using (var fs = File.OpenRead(path))
            data = (RoadsFileXml?)FileSerializer.Deserialize(fs);

        if (data?.Roads is null)
        {
            log.LogWarning("RoadData: roads.xml produced no templates");
            return;
        }

        int id = 0;
        foreach (var xml in data.Roads)
        {
            if (xml.Center is null || xml.P1 is null || xml.P2 is null || xml.RoadExit is null)
            {
                log.LogWarning("RoadData: skipping road '{Name}' — missing center/p1/p2/roadexit element", xml.Name);
                continue;
            }

            _templates.Add(new RoadTemplate
            {
                Id = id++,
                Name = xml.Name ?? "ROAD",
                WorldId = xml.Map,
                Radius = xml.Radius,
                Center = new RoadPoint(xml.Center.X, xml.Center.Y, xml.Center.Z),
                P1 = new RoadPoint(xml.P1.X, xml.P1.Y, xml.P1.Z),
                P2 = new RoadPoint(xml.P2.X, xml.P2.Y, xml.P2.Z),
                ExitWorldId = xml.RoadExit.MapId,
                ExitPoint = new RoadPoint(xml.RoadExit.X, xml.RoadExit.Y, xml.RoadExit.Z),
            });
        }

        log.LogInformation("RoadData: loaded {Count} road(s)", _templates.Count);
    }
}
