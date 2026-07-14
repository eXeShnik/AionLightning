using System.Xml.Serialization;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Model.Templates.Siege;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

[XmlRoot("siege_locations")]
public sealed class SiegeLocationsFileXml
{
    [XmlElement("siege_location")] public List<SiegeLocationTemplate> Locations { get; set; } = [];
}

/// <summary>
/// Loads data/static_data/siege/siege_locations.xml (Java dataholders.SiegeLocationData) and builds
/// the fortress/artifact/outpost/source/all-locations maps consumed by SiegeService. Mirrors Java's
/// afterUnmarshal switch exactly: FORTRESS entries also get an implicit ArtifactLocation "core" at the
/// same id; INDUN/UNDERPASS entries are parsed but not tracked in any runtime map (no runtime
/// SiegeLocation subclass exists for them in Java either).
/// </summary>
public sealed class SiegeLocationData
{
    private static readonly XmlSerializer Serializer = new(typeof(SiegeLocationsFileXml));

    private readonly Dictionary<int, ArtifactLocation> _artifacts = new();
    private readonly Dictionary<int, FortressLocation> _fortresses = new();
    private readonly Dictionary<int, OutpostLocation> _outposts = new();
    private readonly Dictionary<int, SourceLocation> _sources = new();
    private readonly Dictionary<int, SiegeLocation> _locations = new();

    public IReadOnlyDictionary<int, ArtifactLocation> Artifacts => _artifacts;
    public IReadOnlyDictionary<int, FortressLocation> Fortresses => _fortresses;
    public IReadOnlyDictionary<int, OutpostLocation> Outposts => _outposts;
    public IReadOnlyDictionary<int, SourceLocation> Sources => _sources;
    public IReadOnlyDictionary<int, SiegeLocation> Locations => _locations;

    public int Count => _locations.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "siege", "siege_locations.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("SiegeLocationData: siege_locations.xml not found at {Path}", path);
            return;
        }

        SiegeLocationsFileXml? data;
        using (var fs = File.OpenRead(path))
            data = (SiegeLocationsFileXml?)Serializer.Deserialize(fs);

        if (data?.Locations is null)
        {
            log.LogWarning("SiegeLocationData: siege_locations.xml produced no templates");
            return;
        }

        foreach (var template in data.Locations)
        {
            switch (template.Type)
            {
                case SiegeType.FORTRESS:
                    var fortress = new FortressLocation(template);
                    _fortresses[template.Id] = fortress;
                    _locations[template.Id] = fortress;
                    _artifacts[template.Id] = new ArtifactLocation(template);
                    break;
                case SiegeType.ARTIFACT:
                    var artifact = new ArtifactLocation(template);
                    _artifacts[template.Id] = artifact;
                    _locations[template.Id] = artifact;
                    break;
                case SiegeType.BOSSRAID_LIGHT:
                case SiegeType.BOSSRAID_DARK:
                    var outpost = new OutpostLocation(template);
                    _outposts[template.Id] = outpost;
                    _locations[template.Id] = outpost;
                    break;
                case SiegeType.SOURCE:
                    var source = new SourceLocation(template);
                    _sources[template.Id] = source;
                    _locations[template.Id] = source;
                    break;
                default:
                    break; // INDUN / UNDERPASS: not tracked as runtime siege locations (matches Java)
            }
        }

        log.LogInformation(
            "SiegeLocationData: loaded {Count} siege location(s) ({Fortresses} fortresses, {Artifacts} artifacts, {Outposts} outposts, {Sources} sources)",
            _locations.Count, _fortresses.Count, _artifacts.Count, _outposts.Count, _sources.Count);
    }
}
