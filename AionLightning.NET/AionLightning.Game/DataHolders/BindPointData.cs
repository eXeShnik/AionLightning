using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class BindPointData
{
    private readonly Dictionary<int, long> _prices = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "bind_points", "bind_points.xml");
        if (!File.Exists(path)) { log.LogWarning("BindPointData: bind_points.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "bind_point") continue;
            if (!int.TryParse(reader.GetAttribute("npcid"), out int npcId)) continue;
            long.TryParse(reader.GetAttribute("price"), out long price);
            _prices[npcId] = price;
        }

        log.LogInformation("BindPointData: loaded {Count} bind stone prices", _prices.Count);
    }

    public long GetPrice(int npcId) => _prices.GetValueOrDefault(npcId, 0L);
}
