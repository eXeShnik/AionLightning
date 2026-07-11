using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Quest.Script;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads Phase 1 of the quest engine's data-driven templates from
/// <c>quest_script_data/*.xml</c> (Java questEngine data). Only &lt;item_collecting&gt; entries
/// are parsed for now; other sibling element types are added in later C2 phases.
/// </summary>
public sealed class QuestScriptData
{
    private readonly List<ItemCollectingScriptEntry> _itemCollecting = new();

    public IReadOnlyList<ItemCollectingScriptEntry> ItemCollecting => _itemCollecting;

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "quest_script_data");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("QuestScriptData: directory not found: {Path}", dir);
            return;
        }

        var serializer = new XmlSerializer(typeof(QuestScriptsXml));
        foreach (var file in Directory.EnumerateFiles(dir, "*.xml"))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var root = (QuestScriptsXml?)serializer.Deserialize(fs);
                if (root is null) continue;
                _itemCollecting.AddRange(root.ItemCollecting);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "QuestScriptData: failed to parse {File}", file);
            }
        }

        log.LogInformation("QuestScriptData: loaded {Count} item_collecting entries", _itemCollecting.Count);
    }
}
