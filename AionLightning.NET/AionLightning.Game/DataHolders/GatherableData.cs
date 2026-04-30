using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Gatherable;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class GatherableData
{
    private readonly Dictionary<int, GatherableTemplate> _data = new();
    private static readonly XmlSerializer _s = new(typeof(GatherableTemplate));

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "gatherables", "gatherable_templates.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("GatherableData: file not found: {Path}", path);
            return;
        }

        var settings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
        using var reader = XmlReader.Create(path, settings);

        while (reader.ReadToFollowing("gatherable_template"))
        {
            using var sub = reader.ReadSubtree();
            try
            {
                if (_s.Deserialize(sub) is GatherableTemplate t)
                    _data[t.TemplateId] = t;
            }
            catch { /* skip malformed */ }
        }

        log.LogInformation("GatherableData: loaded {Count} gatherable templates", _data.Count);
    }

    public GatherableTemplate? GetTemplate(int templateId) => _data.GetValueOrDefault(templateId);
    public int Count => _data.Count;
}
