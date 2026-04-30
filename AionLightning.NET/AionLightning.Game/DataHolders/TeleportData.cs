using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class TeleportData
{
    public record TeleLocation(int LocId, int MapId, float X, float Y, float Z, byte Heading);
    public record TeleDestination(int LocId, long Price, string Type);

    // loc_id -> location coordinates
    private readonly Dictionary<int, TeleLocation> _locations = new();
    // npcId -> list of destinations this NPC offers
    private readonly Dictionary<int, List<TeleDestination>> _npcDestinations = new();
    // npcId -> teleportId (used by client to look up destination map)
    private readonly Dictionary<int, int> _npcTeleportIds = new();

    public void Load(string dataRoot, ILogger log)
    {
        LoadLocations(dataRoot, log);
        LoadNpcTeleporters(dataRoot, log);
    }

    private void LoadLocations(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "teleport_location.xml");
        if (!File.Exists(path)) { log.LogWarning("TeleportData: teleport_location.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "teleloc_template") continue;
            if (!int.TryParse(reader.GetAttribute("loc_id"), out int locId)) continue;
            if (!int.TryParse(reader.GetAttribute("mapid"),  out int mapId))  continue;
            float.TryParse(reader.GetAttribute("posX"),    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x);
            float.TryParse(reader.GetAttribute("posY"),    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y);
            float.TryParse(reader.GetAttribute("posZ"),    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z);
            byte.TryParse(reader.GetAttribute("heading"),  out byte heading);
            _locations[locId] = new TeleLocation(locId, mapId, x, y, z, heading);
        }

        log.LogInformation("TeleportData: loaded {Count} teleport locations", _locations.Count);
    }

    private void LoadNpcTeleporters(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "npc_teleporter.xml");
        if (!File.Exists(path)) { log.LogWarning("TeleportData: npc_teleporter.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        int[]? currentNpcIds = null;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "teleporter_template")
            {
                var npcIdsAttr = reader.GetAttribute("npc_ids") ?? "";
                currentNpcIds = npcIdsAttr
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : -1)
                    .Where(id => id > 0)
                    .ToArray();

                if (int.TryParse(reader.GetAttribute("teleportId"), out int tpId) && tpId > 0)
                    foreach (var npcId in currentNpcIds)
                        _npcTeleportIds[npcId] = tpId;
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "telelocation" && currentNpcIds != null)
            {
                if (!int.TryParse(reader.GetAttribute("loc_id"), out int locId)) continue;
                long.TryParse(reader.GetAttribute("price"), out long price);
                var type = reader.GetAttribute("type") ?? "REGULAR";

                var dest = new TeleDestination(locId, price, type);
                foreach (var npcId in currentNpcIds)
                {
                    if (!_npcDestinations.TryGetValue(npcId, out var list))
                        _npcDestinations[npcId] = list = new List<TeleDestination>();
                    list.Add(dest);
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "teleporter_template")
            {
                currentNpcIds = null;
            }
        }

        log.LogInformation("TeleportData: loaded teleporter data for {Count} NPCs", _npcDestinations.Count);
    }

    public TeleLocation? GetLocation(int locId)
        => _locations.GetValueOrDefault(locId);

    public IReadOnlyList<TeleDestination>? GetDestinations(int npcId)
        => _npcDestinations.TryGetValue(npcId, out var list) ? list : null;

    public bool IsTeleporter(int npcId) => _npcDestinations.ContainsKey(npcId);

    public TeleDestination? GetDestination(int npcId, int locId)
        => GetDestinations(npcId)?.FirstOrDefault(d => d.LocId == locId);

    public int? GetTeleportId(int npcId)
        => _npcTeleportIds.TryGetValue(npcId, out var id) ? id : null;
}
