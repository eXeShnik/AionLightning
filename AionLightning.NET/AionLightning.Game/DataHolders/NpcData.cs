using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Npc;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class NpcData
{
    private readonly Dictionary<int, NpcTemplate> _data = new();
    private static readonly XmlSerializer _templateSerializer = new(typeof(NpcTemplate));

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "npcs", "npc_templates.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("NpcData: file not found: {Path}", path);
            return;
        }

        var settings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
        using var reader = XmlReader.Create(path, settings);

        while (reader.ReadToFollowing("npc_template"))
        {
            using var sub = reader.ReadSubtree();
            try
            {
                if (_templateSerializer.Deserialize(sub) is NpcTemplate t)
                    _data[t.NpcId] = t;
            }
            catch
            {
                // silently skip malformed entries
            }
        }

        log.LogInformation("NpcData: loaded {Count} NPC templates", _data.Count);
    }

    public NpcTemplate? GetTemplate(int npcId) => _data.GetValueOrDefault(npcId);

    public IEnumerable<NpcTemplate> All => _data.Values;

    public int Count => _data.Count;
}
