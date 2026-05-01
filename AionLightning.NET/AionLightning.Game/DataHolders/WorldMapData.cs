using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class WorldMapData
{
    private readonly Dictionary<int, string> _dropTypes = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "world_maps.xml");
        if (!File.Exists(path)) { log.LogWarning("WorldMapData: world_maps.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "map") continue;
            if (!int.TryParse(reader.GetAttribute("id"), out int mapId)) continue;
            var dropType = reader.GetAttribute("drop_type");
            if (!string.IsNullOrEmpty(dropType))
                _dropTypes[mapId] = dropType;
        }

        log.LogInformation("WorldMapData: loaded {Count} world drop types", _dropTypes.Count);
    }

    public string GetDropType(int worldId) => _dropTypes.GetValueOrDefault(worldId, string.Empty);
}
