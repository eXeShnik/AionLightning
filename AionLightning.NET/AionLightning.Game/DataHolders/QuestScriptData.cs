using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Quest.Script;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads the quest engine's data-driven templates from <c>quest_script_data/*.xml</c> (Java
/// questEngine data). Phase 1 parsed &lt;item_collecting&gt;; Phase 2 adds &lt;monster_hunt&gt;
/// and &lt;report_to&gt;. Other sibling element types are added in later C2 phases.
/// </summary>
public sealed class QuestScriptData
{
    private readonly List<ItemCollectingScriptEntry> _itemCollecting = new();
    private readonly List<MonsterHuntScriptEntry>    _monsterHunt    = new();
    private readonly List<ReportToScriptEntry>       _reportTo       = new();

    public IReadOnlyList<ItemCollectingScriptEntry> ItemCollecting => _itemCollecting;
    public IReadOnlyList<MonsterHuntScriptEntry>    MonsterHunt    => _monsterHunt;
    public IReadOnlyList<ReportToScriptEntry>       ReportTo       => _reportTo;

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
                _monsterHunt.AddRange(root.MonsterHunt);
                _reportTo.AddRange(root.ReportTo);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "QuestScriptData: failed to parse {File}", file);
            }
        }

        log.LogInformation(
            "QuestScriptData: loaded {ItemCollecting} item_collecting, {MonsterHunt} monster_hunt, {ReportTo} report_to entries",
            _itemCollecting.Count, _monsterHunt.Count, _reportTo.Count);
    }
}
