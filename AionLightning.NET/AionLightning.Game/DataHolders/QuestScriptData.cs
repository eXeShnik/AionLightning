using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Quest.Script;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads the quest engine's data-driven templates from <c>quest_script_data/*.xml</c> (Java
/// questEngine data). Phase 1 parsed &lt;item_collecting&gt;; Phase 2 added &lt;monster_hunt&gt;
/// and &lt;report_to&gt;; Phase 3 adds &lt;report_to_many&gt;, &lt;kill_in_world&gt;,
/// &lt;kill_spawned&gt; and &lt;work_order&gt;. Other sibling element types are added in later
/// C2 phases.
/// </summary>
public sealed class QuestScriptData
{
    private readonly List<ItemCollectingScriptEntry> _itemCollecting = new();
    private readonly List<MonsterHuntScriptEntry>    _monsterHunt    = new();
    private readonly List<ReportToScriptEntry>       _reportTo       = new();
    private readonly List<ReportToManyScriptEntry>   _reportToMany   = new();
    private readonly List<KillInWorldScriptEntry>    _killInWorld    = new();
    private readonly List<KillSpawnedScriptEntry>    _killSpawned    = new();
    private readonly List<WorkOrderScriptEntry>      _workOrders     = new();

    public IReadOnlyList<ItemCollectingScriptEntry> ItemCollecting => _itemCollecting;
    public IReadOnlyList<MonsterHuntScriptEntry>    MonsterHunt    => _monsterHunt;
    public IReadOnlyList<ReportToScriptEntry>       ReportTo       => _reportTo;
    public IReadOnlyList<ReportToManyScriptEntry>   ReportToMany   => _reportToMany;
    public IReadOnlyList<KillInWorldScriptEntry>    KillInWorld    => _killInWorld;
    public IReadOnlyList<KillSpawnedScriptEntry>    KillSpawned    => _killSpawned;
    public IReadOnlyList<WorkOrderScriptEntry>      WorkOrders     => _workOrders;

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
                _reportToMany.AddRange(root.ReportToMany);
                _killInWorld.AddRange(root.KillInWorld);
                _killSpawned.AddRange(root.KillSpawned);
                _workOrders.AddRange(root.WorkOrders);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "QuestScriptData: failed to parse {File}", file);
            }
        }

        log.LogInformation(
            "QuestScriptData: loaded {ItemCollecting} item_collecting, {MonsterHunt} monster_hunt, {ReportTo} report_to, " +
            "{ReportToMany} report_to_many, {KillInWorld} kill_in_world, {KillSpawned} kill_spawned, {WorkOrders} work_order entries",
            _itemCollecting.Count, _monsterHunt.Count, _reportTo.Count,
            _reportToMany.Count, _killInWorld.Count, _killSpawned.Count, _workOrders.Count);

        int itemStartCount = _reportToMany.Count(e => e.StartItemId != 0);
        if (itemStartCount > 0)
            log.LogWarning(
                "QuestScriptData: {Count} report_to_many entries use start_item_id — this alternate " +
                "start trigger is not wired (no quest-item-use dialog event exists yet); they only " +
                "start via a normal NPC talk", itemStartCount);
    }
}
