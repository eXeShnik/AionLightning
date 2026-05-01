using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class PortalData
{
    public readonly record struct PortalLocation(int WorldId, float X, float Y, float Z, byte Heading);

    private readonly record struct PortalPath(int LocId, string Race);

    private readonly Dictionary<int, PortalLocation> _locations = new();
    private readonly Dictionary<int, List<PortalPath>> _portals  = new();

    public void Load(string dataRoot, ILogger log)
    {
        LoadLocations(dataRoot, log);
        LoadPortals(dataRoot, log);
    }

    private void LoadLocations(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "portals", "portal_loc.xml");
        if (!File.Exists(path)) { log.LogWarning("PortalData: portal_loc.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "portal_loc") continue;
            if (!int.TryParse(reader.GetAttribute("loc_id"),   out int locId))   continue;
            if (!int.TryParse(reader.GetAttribute("world_id"), out int worldId)) continue;
            float.TryParse(reader.GetAttribute("x"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x);
            float.TryParse(reader.GetAttribute("y"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y);
            float.TryParse(reader.GetAttribute("z"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z);
            byte.TryParse(reader.GetAttribute("h"), out byte heading);
            _locations[locId] = new PortalLocation(worldId, x, y, z, heading);
        }

        log.LogInformation("PortalData: loaded {Count} portal locations", _locations.Count);
    }

    private void LoadPortals(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "portals", "portal_template2.xml");
        if (!File.Exists(path)) { log.LogWarning("PortalData: portal_template2.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        int currentNpcId = 0;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "portal_use")
            {
                int.TryParse(reader.GetAttribute("npc_id"), out currentNpcId);
                if (!_portals.ContainsKey(currentNpcId))
                    _portals[currentNpcId] = new List<PortalPath>();
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "portal_path" && currentNpcId > 0)
            {
                if (!int.TryParse(reader.GetAttribute("loc_id"), out int locId)) continue;
                var race = reader.GetAttribute("race") ?? string.Empty;
                _portals[currentNpcId].Add(new PortalPath(locId, race));
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "portal_use")
            {
                currentNpcId = 0;
            }
        }

        log.LogInformation("PortalData: loaded portal data for {Count} NPCs", _portals.Count);
    }

    public bool IsPortal(int npcId) => _portals.ContainsKey(npcId);

    public PortalLocation? GetPortalLocation(int npcId, Model.Race playerRace)
    {
        if (!_portals.TryGetValue(npcId, out var paths)) return null;

        var raceStr = playerRace == Model.Race.ELYOS ? "ELYOS" : "ASMODIANS";

        // Prefer race-specific path; fall back to unrestricted (empty race attribute)
        PortalPath? best = null;
        foreach (var p in paths)
        {
            if (string.IsNullOrEmpty(p.Race))
            {
                best ??= p;
            }
            else if (p.Race.Equals(raceStr, StringComparison.OrdinalIgnoreCase))
            {
                best = p;
                break;
            }
        }

        if (best is null) return null;
        return _locations.GetValueOrDefault(best.Value.LocId);
    }
}
