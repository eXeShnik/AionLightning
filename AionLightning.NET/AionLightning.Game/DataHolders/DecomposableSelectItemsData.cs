using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Decomposable;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

[XmlRoot("decomposable_selectitems")]
public sealed class DecomposableSelectItemsData
{
    // itemId → (playerClass → SelectItems)
    private readonly Dictionary<int, Dictionary<PlayerClass, SelectItems>> _data = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "decomposable_items", "decomposable_selectitems.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("DecomposableSelectItemsData: file not found: {Path}", path);
            return;
        }

        var serializer = new XmlSerializer(typeof(DecomposableSelectItemsRoot));
        var settings   = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
        using var reader = XmlReader.Create(path, settings);

        var root = (DecomposableSelectItemsRoot?)serializer.Deserialize(reader);
        if (root?.Items is null) return;

        foreach (var entry in root.Items)
        {
            if (!_data.TryGetValue(entry.ItemId, out var byClass))
                _data[entry.ItemId] = byClass = new Dictionary<PlayerClass, SelectItems>();

            foreach (var si in entry.SelectItems)
                byClass[si.PlayerClass] = si;
        }

        log.LogInformation("DecomposableSelectItemsData: loaded {Count} selectable item entries", _data.Count);
    }

    public SelectItems? GetSelectItems(PlayerClass playerClass, int itemId)
    {
        if (!_data.TryGetValue(itemId, out var byClass)) return null;
        if (byClass.TryGetValue(playerClass, out var exact)) return exact;
        return byClass.GetValueOrDefault(PlayerClass.ALL);
    }
}

[XmlRoot("decomposable_selectitems")]
public sealed class DecomposableSelectItemsRoot
{
    [XmlElement("decomposable_selectitem")] public List<DecomposableSelectItem> Items { get; set; } = new();
}
