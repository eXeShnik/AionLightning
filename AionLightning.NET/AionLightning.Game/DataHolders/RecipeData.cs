using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Recipe;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class RecipeData
{
    private readonly Dictionary<int, RecipeTemplate> _data = new();

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "recipe", "recipe_templates.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("RecipeData: file not found: {Path}", path);
            return;
        }

        var serializer = new XmlSerializer(typeof(RecipeListXml));
        using var fs   = File.OpenRead(path);
        var root       = (RecipeListXml?)serializer.Deserialize(fs);
        if (root is null) return;

        foreach (var t in root.Items)
            _data[t.Id] = t;

        log.LogInformation("RecipeData: loaded {Count} recipe templates", _data.Count);
    }

    public RecipeTemplate? GetTemplate(int id) => _data.GetValueOrDefault(id);

    /// <summary>Returns IDs of all auto-learn recipes available to the given race string (e.g. "ELYOS").</summary>
    public IEnumerable<int> GetAutoLearnIds(string race)
        => _data.Values
            .Where(t => t.AutoLearn == 1 &&
                        (string.Equals(t.Race, race, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t.Race, "PC_ALL", StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.Id);

    public int Count => _data.Count;

    [XmlRoot("recipe_templates")]
    private sealed class RecipeListXml
    {
        [XmlElement("recipe_template")]
        public List<RecipeTemplate> Items { get; set; } = new();
    }
}
