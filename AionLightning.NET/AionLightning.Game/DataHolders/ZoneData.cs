using System.Xml.Serialization;
using AionLightning.Game.Model.Zone;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

// XML binding types for data/static_data/zones/zones_*.xml (Java ZoneReader/zones.xsd port —
// only the geometry needed for area-membership tests; priority/flags/siege_id/town_id are not
// consumed by this port's simplified ZoneRegion model).

[XmlRoot("zones")]
public sealed class ZonesFileXml
{
    [XmlElement("zone")] public List<ZoneXml> Zones { get; set; } = new();
}

public sealed class ZoneXml
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("mapid")] public int MapId { get; set; }
    [XmlElement("points")] public PointsXml? Points { get; set; }
    [XmlElement("cylinder")] public CylinderXml? Cylinder { get; set; }
    [XmlElement("sphere")] public SphereXml? Sphere { get; set; }
    [XmlElement("semisphere")] public SphereXml? Semisphere { get; set; }
}

public sealed class PointsXml
{
    [XmlAttribute("top")] public float Top { get; set; }
    [XmlAttribute("bottom")] public float Bottom { get; set; }
    [XmlElement("point")] public List<PointXml> Point { get; set; } = new();
}

public sealed class PointXml
{
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
}

public sealed class CylinderXml
{
    [XmlAttribute("top")] public float Top { get; set; }
    [XmlAttribute("bottom")] public float Bottom { get; set; }
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("r")] public float R { get; set; }
}

public sealed class SphereXml
{
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("r")] public float R { get; set; }
}

/// <summary>
/// Loads every <c>zones_*.xml</c> into <see cref="ZoneRegion"/>s keyed by world id (Java
/// <c>dataholders.ZoneData</c> + <c>ZoneService.zoneByMapIdMap</c> port). Only POLYGON, CYLINDER
/// and SPHERE areas are supported (the only ones present in the ported data set); SEMISPHERE and
/// zones with no recognized geometry are skipped with a log line rather than crashing.
/// </summary>
public sealed class ZoneData
{
    private readonly Dictionary<int, List<ZoneRegion>> _byWorld = new();
    private readonly Dictionary<string, ZoneRegion> _byName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly XmlSerializer _fileSerializer = new(typeof(ZonesFileXml));

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "zones");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("ZoneData: directory not found: {Dir}", dir);
            return;
        }

        int totalZones = 0;
        int skipped = 0;
        foreach (var file in Directory.GetFiles(dir, "zones_*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var data = (ZonesFileXml?)_fileSerializer.Deserialize(fs);
                if (data?.Zones is null) continue;

                foreach (var zoneXml in data.Zones)
                {
                    var region = BuildRegion(zoneXml, log);
                    if (region is null)
                    {
                        skipped++;
                        continue;
                    }

                    if (!_byWorld.TryGetValue(region.WorldId, out var list))
                        _byWorld[region.WorldId] = list = new List<ZoneRegion>();
                    list.Add(region);
                    _byName[region.Name] = region;
                    totalZones++;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "ZoneData: failed to parse {File}", Path.GetFileName(file));
            }
        }

        log.LogInformation("ZoneData: loaded {Count} zone regions across {Worlds} world(s) ({Skipped} skipped)",
            totalZones, _byWorld.Count, skipped);
    }

    private static ZoneRegion? BuildRegion(ZoneXml zone, ILogger log)
    {
        if (zone.Points is not null && zone.Points.Point.Count >= 3)
        {
            return new ZoneRegion
            {
                Name = zone.Name,
                WorldId = zone.MapId,
                AreaType = ZoneAreaType.Polygon,
                MinZ = zone.Points.Bottom,
                MaxZ = zone.Points.Top,
                PolyX = zone.Points.Point.Select(p => p.X).ToArray(),
                PolyY = zone.Points.Point.Select(p => p.Y).ToArray(),
            };
        }

        if (zone.Cylinder is not null)
        {
            return new ZoneRegion
            {
                Name = zone.Name,
                WorldId = zone.MapId,
                AreaType = ZoneAreaType.Cylinder,
                MinZ = zone.Cylinder.Bottom,
                MaxZ = zone.Cylinder.Top,
                CenterX = zone.Cylinder.X,
                CenterY = zone.Cylinder.Y,
                Radius = zone.Cylinder.R,
            };
        }

        if (zone.Sphere is not null)
        {
            return new ZoneRegion
            {
                Name = zone.Name,
                WorldId = zone.MapId,
                AreaType = ZoneAreaType.Sphere,
                CenterX = zone.Sphere.X,
                CenterY = zone.Sphere.Y,
                CenterZ = zone.Sphere.Z,
                Radius = zone.Sphere.R,
            };
        }

        if (zone.Semisphere is not null)
        {
            log.LogDebug("ZoneData: skipping unhandled SEMISPHERE zone {Name}", zone.Name);
            return null;
        }

        log.LogDebug("ZoneData: zone {Name} has no recognized area geometry, skipping", zone.Name);
        return null;
    }

    public IReadOnlyList<ZoneRegion> GetRegionsForWorld(int worldId)
        => _byWorld.TryGetValue(worldId, out var list) ? list : Array.Empty<ZoneRegion>();

    public ZoneRegion? GetByName(string name)
        => _byName.TryGetValue(name, out var region) ? region : null;
}
