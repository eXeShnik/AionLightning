using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Quest;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class QuestData
{
    private readonly Dictionary<int, QuestTemplate> _data = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "quest_data", "quest_data.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("QuestData: file not found: {Path}", path);
            return;
        }

        var serializer = new XmlSerializer(typeof(QuestsXml));
        using var fs   = File.OpenRead(path);
        var root       = (QuestsXml?)serializer.Deserialize(fs);
        if (root is null) return;

        foreach (var t in root.Items)
            _data[t.Id] = t;

        log.LogInformation("QuestData: loaded {Count} quest templates", _data.Count);
    }

    public QuestTemplate? GetTemplate(int id) => _data.GetValueOrDefault(id);
    public int Count => _data.Count;

    [XmlRoot("quests")]
    public sealed class QuestsXml
    {
        [XmlElement("quest")]
        public List<QuestTemplate> Items { get; set; } = new();
    }
}
