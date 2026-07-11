using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Quest.Script;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads the quest engine's data-driven templates from <c>quest_script_data/*.xml</c> (Java
/// questEngine data). Phase 1 parsed &lt;item_collecting&gt;; Phase 2 added &lt;monster_hunt&gt;
/// and &lt;report_to&gt;; Phase 3 added &lt;report_to_many&gt;, &lt;kill_in_world&gt;,
/// &lt;kill_spawned&gt; and &lt;work_order&gt;; Phase 4 adds &lt;crafting_rewards&gt;,
/// &lt;relic_rewards&gt;, &lt;fountain_rewards&gt;, &lt;skill_use&gt; and
/// &lt;mentor_monster_hunt&gt;. Other sibling element types are added in later C2 phases.
/// </summary>
public sealed class QuestScriptData
{
    private readonly List<ItemCollectingScriptEntry>    _itemCollecting    = new();
    private readonly List<MonsterHuntScriptEntry>       _monsterHunt       = new();
    private readonly List<ReportToScriptEntry>          _reportTo          = new();
    private readonly List<ReportToManyScriptEntry>      _reportToMany      = new();
    private readonly List<KillInWorldScriptEntry>       _killInWorld       = new();
    private readonly List<KillSpawnedScriptEntry>       _killSpawned       = new();
    private readonly List<WorkOrderScriptEntry>         _workOrders        = new();
    private readonly List<CraftingRewardsScriptEntry>   _craftingRewards   = new();
    private readonly List<RelicRewardsScriptEntry>      _relicRewards      = new();
    private readonly List<FountainRewardsScriptEntry>   _fountainRewards   = new();
    private readonly List<SkillUseScriptEntry>          _skillUse          = new();
    private readonly List<MentorMonsterHuntScriptEntry> _mentorMonsterHunt = new();

    public IReadOnlyList<ItemCollectingScriptEntry>    ItemCollecting    => _itemCollecting;
    public IReadOnlyList<MonsterHuntScriptEntry>       MonsterHunt       => _monsterHunt;
    public IReadOnlyList<ReportToScriptEntry>          ReportTo          => _reportTo;
    public IReadOnlyList<ReportToManyScriptEntry>      ReportToMany      => _reportToMany;
    public IReadOnlyList<KillInWorldScriptEntry>       KillInWorld       => _killInWorld;
    public IReadOnlyList<KillSpawnedScriptEntry>       KillSpawned       => _killSpawned;
    public IReadOnlyList<WorkOrderScriptEntry>         WorkOrders        => _workOrders;
    public IReadOnlyList<CraftingRewardsScriptEntry>   CraftingRewards   => _craftingRewards;
    public IReadOnlyList<RelicRewardsScriptEntry>      RelicRewards      => _relicRewards;
    public IReadOnlyList<FountainRewardsScriptEntry>   FountainRewards   => _fountainRewards;
    public IReadOnlyList<SkillUseScriptEntry>          SkillUse          => _skillUse;
    public IReadOnlyList<MentorMonsterHuntScriptEntry> MentorMonsterHunt => _mentorMonsterHunt;

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
                _craftingRewards.AddRange(root.CraftingRewards);
                _relicRewards.AddRange(root.RelicRewards);
                _fountainRewards.AddRange(root.FountainRewards);
                _skillUse.AddRange(root.SkillUse);
                _mentorMonsterHunt.AddRange(root.MentorMonsterHunt);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "QuestScriptData: failed to parse {File}", file);
            }
        }

        log.LogInformation(
            "QuestScriptData: loaded {ItemCollecting} item_collecting, {MonsterHunt} monster_hunt, {ReportTo} report_to, " +
            "{ReportToMany} report_to_many, {KillInWorld} kill_in_world, {KillSpawned} kill_spawned, {WorkOrders} work_order, " +
            "{CraftingRewards} crafting_rewards, {RelicRewards} relic_rewards, {FountainRewards} fountain_rewards, " +
            "{SkillUse} skill_use, {MentorMonsterHunt} mentor_monster_hunt entries",
            _itemCollecting.Count, _monsterHunt.Count, _reportTo.Count,
            _reportToMany.Count, _killInWorld.Count, _killSpawned.Count, _workOrders.Count,
            _craftingRewards.Count, _relicRewards.Count, _fountainRewards.Count,
            _skillUse.Count, _mentorMonsterHunt.Count);

        int itemStartCount = _reportToMany.Count(e => e.StartItemId != 0);
        if (itemStartCount > 0)
            log.LogWarning(
                "QuestScriptData: {Count} report_to_many entries use start_item_id — this alternate " +
                "start trigger is not wired (no quest-item-use dialog event exists yet); they only " +
                "start via a normal NPC talk", itemStartCount);
    }
}
