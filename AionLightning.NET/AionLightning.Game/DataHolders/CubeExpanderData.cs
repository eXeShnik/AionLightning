using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class CubeExpanderData
{
    // npcId → (expandLevel → price)
    private readonly Dictionary<int, Dictionary<int, long>> _npcs = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "cube_expander", "cube_expander.xml");
        if (!File.Exists(path)) { log.LogWarning("CubeExpanderData: cube_expander.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        int currentNpcId = 0;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "cube_npc")
            {
                if (int.TryParse(reader.GetAttribute("id"), out int npcId))
                {
                    currentNpcId = npcId;
                    _npcs[npcId] = new Dictionary<int, long>();
                }
                continue;
            }
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "expand" && currentNpcId != 0)
            {
                if (int.TryParse(reader.GetAttribute("level"), out int level)
                    && long.TryParse(reader.GetAttribute("price"), out long price))
                {
                    _npcs[currentNpcId][level] = price;
                }
                continue;
            }
            if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "cube_npc")
                currentNpcId = 0;
        }

        log.LogInformation("CubeExpanderData: loaded {Count} cube expander NPCs", _npcs.Count);
    }

    /// <summary>Returns the price for expanding to the next cube level, or null if this NPC doesn't offer it.</summary>
    public long? GetExpandPrice(int npcId, int nextLevel)
    {
        if (!_npcs.TryGetValue(npcId, out var levels)) return null;
        return levels.TryGetValue(nextLevel, out long price) ? price : null;
    }

    public bool IsCubeExpander(int npcId) => _npcs.ContainsKey(npcId);
}
