using System.Xml;
using AionLightning.Game.Model.Templates.FlyRing;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads <c>fly_rings/fly_rings.xml</c> — flight-training rings keyed by name and indexed by world
/// (Java <c>FlyRingData</c>). Consumed by the move-time onPassFlyingRing check in
/// <c>ZoneService</c>.
/// </summary>
public sealed class FlyRingData
{
    private readonly Dictionary<string, FlyRingTemplate> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<FlyRingTemplate>> _byWorld = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "fly_rings", "fly_rings.xml");
        if (!File.Exists(path)) { log.LogWarning("FlyRingData: fly_rings.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        string? name = null; int worldId = 0; float radius = 0, cx = 0, cy = 0, cz = 0;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "fly_ring")
            {
                name    = reader.GetAttribute("name");
                worldId = int.TryParse(reader.GetAttribute("map"), out int w) ? w : 0;
                radius  = float.TryParse(reader.GetAttribute("radius"), System.Globalization.CultureInfo.InvariantCulture, out float r) ? r : 0f;
                cx = cy = cz = 0f;
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "center")
            {
                cx = ParseF(reader.GetAttribute("x"));
                cy = ParseF(reader.GetAttribute("y"));
                cz = ParseF(reader.GetAttribute("z"));
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "fly_ring" && name is not null)
            {
                var ring = new FlyRingTemplate(name, worldId, radius, cx, cy, cz);
                _byName[name] = ring;
                if (!_byWorld.TryGetValue(worldId, out var list)) { list = new(); _byWorld[worldId] = list; }
                list.Add(ring);
                name = null;
            }
        }

        log.LogInformation("FlyRingData: loaded {Count} fly rings across {Worlds} worlds", _byName.Count, _byWorld.Count);
    }

    public IReadOnlyList<FlyRingTemplate> GetRingsForWorld(int worldId)
        => _byWorld.TryGetValue(worldId, out var list) ? list : Array.Empty<FlyRingTemplate>();

    public int Count => _byName.Count;

    private static float ParseF(string? s)
        => float.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;
}
