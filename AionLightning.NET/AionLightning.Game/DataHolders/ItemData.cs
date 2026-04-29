using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Item;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class ItemData
{
    private readonly Dictionary<int, ItemTemplate> _data = new();
    private static readonly XmlSerializer _templateSerializer = new(typeof(ItemTemplate));

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "items", "item_templates.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("ItemData: file not found: {Path}", path);
            return;
        }

        var settings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
        using var reader = XmlReader.Create(path, settings);

        while (reader.ReadToFollowing("item_template"))
        {
            using var sub = reader.ReadSubtree();
            try
            {
                if (_templateSerializer.Deserialize(sub) is ItemTemplate t)
                    _data[t.Id] = t;
            }
            catch
            {
                // silently skip malformed entries
            }
        }

        log.LogInformation("ItemData: loaded {Count} item templates", _data.Count);
    }

    public ItemTemplate? GetTemplate(int itemId) => _data.GetValueOrDefault(itemId);

    public int Count => _data.Count;
}
