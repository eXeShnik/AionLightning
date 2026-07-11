using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Quest;

namespace AionLightning.Game.Model.Templates.Quest.Script;

/// <summary>
/// Root of a <c>quest_script_data/*.xml</c> file. Java's XSD interleaves many sibling element
/// types (report_to, monster_hunt, ...); Phase 1-3 map item_collecting/monster_hunt/report_to/
/// report_to_many/kill_in_world/kill_spawned/work_order; Phase 4 adds crafting_rewards/
/// relic_rewards/fountain_rewards/skill_use/mentor_monster_hunt — other elements are simply
/// skipped by <see cref="XmlSerializer"/>. <c>work_order.xml</c> lives in this same directory (not
/// quest_data/), so it is picked up by the existing directory scan with no extra load step.
/// </summary>
[XmlRoot("quest_scripts")]
public sealed class QuestScriptsXml
{
    [XmlElement("item_collecting")]
    public List<ItemCollectingScriptEntry> ItemCollecting { get; set; } = new();

    [XmlElement("monster_hunt")]
    public List<MonsterHuntScriptEntry> MonsterHunt { get; set; } = new();

    [XmlElement("report_to")]
    public List<ReportToScriptEntry> ReportTo { get; set; } = new();

    [XmlElement("report_to_many")]
    public List<ReportToManyScriptEntry> ReportToMany { get; set; } = new();

    [XmlElement("kill_in_world")]
    public List<KillInWorldScriptEntry> KillInWorld { get; set; } = new();

    [XmlElement("kill_spawned")]
    public List<KillSpawnedScriptEntry> KillSpawned { get; set; } = new();

    [XmlElement("work_order")]
    public List<WorkOrderScriptEntry> WorkOrders { get; set; } = new();

    [XmlElement("crafting_rewards")]
    public List<CraftingRewardsScriptEntry> CraftingRewards { get; set; } = new();

    [XmlElement("relic_rewards")]
    public List<RelicRewardsScriptEntry> RelicRewards { get; set; } = new();

    [XmlElement("fountain_rewards")]
    public List<FountainRewardsScriptEntry> FountainRewards { get; set; } = new();

    [XmlElement("skill_use")]
    public List<SkillUseScriptEntry> SkillUse { get; set; } = new();

    [XmlElement("mentor_monster_hunt")]
    public List<MentorMonsterHuntScriptEntry> MentorMonsterHunt { get; set; } = new();
}

/// <summary>Shared space-separated-id-list parser for the new (Phase 2) entry types below.</summary>
internal static class QuestScriptIds
{
    public static HashSet<int> Parse(string raw)
    {
        var ids = new HashSet<int>(
            raw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
        ids.Remove(0);
        return ids;
    }
}

/// <summary>
/// Port of Java <c>ItemCollectingData</c> (questEngine.handlers.models). Attribute names follow
/// the actual quest_script_data.xsd (start_dialog_id/start_dialog_id2 — the Java model's
/// "HACTION_QUEST_SELECT_id" attribute names are stale and do not match the shipped XML/XSD).
/// </summary>
public sealed class ItemCollectingScriptEntry
{
    [XmlAttribute("id")]               public int    Id               { get; set; }
    [XmlAttribute("start_npc_ids")]    public string StartNpcIdsRaw    { get; set; } = "0";
    [XmlAttribute("next_npc_id")]      public int    NextNpcId         { get; set; }
    [XmlAttribute("action_item_ids")]  public string ActionItemIdsRaw  { get; set; } = "0";
    [XmlAttribute("end_npc_ids")]      public string EndNpcIdsRaw      { get; set; } = "0";
    [XmlAttribute("start_dialog_id")]  public int    StartDialogId     { get; set; }
    [XmlAttribute("start_dialog_id2")] public int    StartDialogId2    { get; set; }
    [XmlAttribute("item_id")]          public int    ItemId            { get; set; }
    [XmlAttribute("movie")]            public int    Movie             { get; set; }

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _actionItemIds;
    private HashSet<int>? _endNpcIds;

    // [XmlIgnore] required: XmlSerializer invokes public get-only collection getters during
    // deserialization BEFORE the Raw attribute strings are assigned, permanently caching an
    // empty parse via the ??= memoization (bug found by the Phase 2 probe harness).
    /// <summary>Start NPC template ids (0 = placeholder, stripped — matches Java's <c>startNpcs.remove(0)</c>).</summary>
    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= ParseIds(StartNpcIdsRaw);

    /// <summary>Action item ids (npcs the player interacts with mid-quest); empty when not declared.</summary>
    [XmlIgnore]
    public HashSet<int> ActionItemIds => _actionItemIds ??= ParseIds(ActionItemIdsRaw);

    /// <summary>End (turn-in) NPC ids; falls back to <see cref="StartNpcIds"/> when not declared (Java parity).</summary>
    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= ParseIds(EndNpcIdsRaw) is { Count: > 0 } explicitEnds
        ? explicitEnds
        : new HashSet<int>(StartNpcIds);

    private static HashSet<int> ParseIds(string raw)
    {
        var ids = new HashSet<int>(
            raw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
        ids.Remove(0);
        return ids;
    }
}

/// <summary>
/// Port of Java <c>MonsterHuntData</c> (questEngine.handlers.models). Attribute names verified
/// against quest_script_data.xsd/actual XML — <c>start_dialog_id</c>/<c>end_dialog_id</c>, not the
/// Java model's stale <c>HACTION_QUEST_SELECT_id</c> field name.
/// </summary>
public sealed class MonsterHuntScriptEntry
{
    [XmlAttribute("id")]               public int    Id                { get; set; }
    [XmlAttribute("start_npc_ids")]    public string StartNpcIdsRaw     { get; set; } = "0";
    [XmlAttribute("end_npc_ids")]      public string EndNpcIdsRaw       { get; set; } = "0";
    [XmlAttribute("start_dialog_id")]  public int    StartDialogId      { get; set; }
    [XmlAttribute("end_dialog_id")]    public int    EndDialogId        { get; set; }
    [XmlAttribute("aggro_start_npcs")] public string AggroStartNpcsRaw  { get; set; } = "0";
    [XmlAttribute("invasion_world")]   public int    InvasionWorld      { get; set; }

    [XmlElement("monster")] public List<MonsterEntry> Monsters { get; set; } = new();

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _endNpcIds;
    private HashSet<int>? _aggroStartNpcIds;

    // [XmlIgnore] is required here: without it, XmlSerializer treats these public get-only
    // HashSet<int> properties as serializable collection members and invokes their getters while
    // building the object graph — before the XmlAttribute-bound "...Raw" strings are assigned —
    // which permanently caches an empty parse via the `??=` memoization below.

    /// <summary>Start NPC template ids (0 = placeholder, stripped). Empty for invasion/aggro-only quests.</summary>
    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);

    /// <summary>End (turn-in) NPC ids; falls back to <see cref="StartNpcIds"/> when not declared (Java parity).</summary>
    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= QuestScriptIds.Parse(EndNpcIdsRaw) is { Count: > 0 } explicitEnds
        ? explicitEnds
        : new HashSet<int>(StartNpcIds);

    /// <summary>
    /// NPCs whose aggro list, once the player is added to it, auto-starts this quest (Java
    /// <c>onAddAggroListEvent</c>). Parsed for data completeness; not wired to an engine event in
    /// this phase — see <see cref="AionLightning.Game.QuestEngine.Handlers.Templates.MonsterHuntHandler"/>.
    /// </summary>
    [XmlIgnore]
    public HashSet<int> AggroStartNpcIds => _aggroStartNpcIds ??= QuestScriptIds.Parse(AggroStartNpcsRaw);
}

/// <summary>
/// Port of Java <c>Monster</c> (a kill-goal group within a &lt;monster_hunt&gt; entry). One quest
/// var (or a contiguous span of vars, for goals over 63 kills) tracks this group's kill count.
/// </summary>
public sealed class MonsterEntry
{
    [XmlAttribute("var")]       public int    Var       { get; set; }
    [XmlAttribute("start_var")] public int    StartVar   { get; set; } // parsed for parity; unused by Java's own handler logic
    [XmlAttribute("end_var")]   public int    EndVar     { get; set; }
    [XmlAttribute("npc_ids")]   public string NpcIdsRaw  { get; set; } = "0";
    [XmlAttribute("npc_seq")]   public int    NpcSeq     { get; set; } // CustomConfig.QUESTDATA_MONSTER_KILLS npc_seq matching not ported (see MonsterHuntHandler remarks)

    private HashSet<int>? _npcIds;
    [XmlIgnore]
    public HashSet<int> NpcIds => _npcIds ??= QuestScriptIds.Parse(NpcIdsRaw);
}

/// <summary>Port of Java <c>ReportToData</c> (questEngine.handlers.models) — "deliver an item / talk to npc" quests.</summary>
public sealed class ReportToScriptEntry
{
    [XmlAttribute("id")]            public int    Id             { get; set; }
    [XmlAttribute("start_npc_ids")] public string StartNpcIdsRaw  { get; set; } = "0";
    [XmlAttribute("end_npc_ids")]   public string EndNpcIdsRaw    { get; set; } = "0";
    [XmlAttribute("item_id")]       public int    ItemId          { get; set; }

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _endNpcIds;

    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);

    /// <summary>End (turn-in) NPC ids. Always explicitly declared per the XSD (Java has no start-npc fallback here).</summary>
    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= QuestScriptIds.Parse(EndNpcIdsRaw);
}

/// <summary>
/// Port of Java <c>ReportToManyData</c> (questEngine.handlers.models) — sequential multi-NPC
/// "report to each in turn" quests. Each &lt;npc_infos&gt; child pins one NPC to a specific step
/// (<c>var</c>); the player must visit them in ascending order before the end NPC(s) accept turn-in.
/// Attribute names verified against quest_script_data.xsd — <c>start_dialog_id</c> (default 1011,
/// applied in the handler, not here) not the Java model's stale <c>HACTION_QUEST_SELECT_id</c>.
/// </summary>
public sealed class ReportToManyScriptEntry
{
    [XmlAttribute("id")]              public int    Id              { get; set; }
    [XmlAttribute("start_npc_ids")]   public string StartNpcIdsRaw   { get; set; } = "0";
    [XmlAttribute("start_item_id")]   public int    StartItemId      { get; set; }
    [XmlAttribute("start_dialog_id")] public int    StartDialogId    { get; set; }
    [XmlAttribute("end_dialog_id")]   public int    EndDialogId      { get; set; }
    [XmlAttribute("end_npc_ids")]     public string EndNpcIdsRaw     { get; set; } = "0";

    [XmlElement("npc_infos")] public List<ReportToManyNpcInfo> NpcInfos { get; set; } = new();

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _endNpcIds;

    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);

    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= QuestScriptIds.Parse(EndNpcIdsRaw);

    /// <summary>Highest npc_infos <c>var</c> (Java's <c>maxVar</c>) — the "reported to everyone" threshold.</summary>
    [XmlIgnore]
    public int MaxVar => NpcInfos.Count == 0 ? 0 : NpcInfos.Max(n => n.Var);
}

/// <summary>Port of Java <c>NpcInfos</c> — one step-npc within a &lt;report_to_many&gt; sequence.</summary>
public sealed class ReportToManyNpcInfo
{
    [XmlAttribute("npc_id")]       public int NpcId       { get; set; }
    [XmlAttribute("var")]          public int Var         { get; set; }
    [XmlAttribute("quest_dialog")] public int QuestDialog  { get; set; }
    [XmlAttribute("close_dialog")] public int CloseDialog  { get; set; }
    // Parsed for data completeness only — no SM_MOVIE-equivalent packet exists yet in this port
    // (same gap already noted for MonsterHuntScriptEntry/ItemCollectingScriptEntry's Movie fields).
    [XmlAttribute("movie")]        public int Movie        { get; set; }
}

/// <summary>
/// Port of Java <c>KillInWorldData</c> (questEngine.handlers.models) — "kill N while in these
/// worlds" daily/repeatable quests. See
/// <see cref="AionLightning.Game.QuestEngine.Handlers.Templates.KillInWorldHandler"/> remarks for
/// why the kill-count objective itself isn't wired in this phase. <c>worlds="0"</c> (or omitted)
/// means "every world map" (Java expands it against WorldMapTemplate at register time); parsed here
/// as an empty set, matching the same 0-is-a-placeholder convention used by every id-list attribute.
/// </summary>
public sealed class KillInWorldScriptEntry
{
    [XmlAttribute("id")]             public int    Id             { get; set; }
    [XmlAttribute("start_npc_ids")]  public string StartNpcIdsRaw  { get; set; } = "0";
    [XmlAttribute("end_npc_ids")]    public string EndNpcIdsRaw    { get; set; } = "0";
    [XmlAttribute("amount")]         public int    Amount          { get; set; }
    [XmlAttribute("worlds")]         public string WorldIdsRaw     { get; set; } = "0";
    [XmlAttribute("invasion_world")] public int    InvasionWorld   { get; set; }

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _endNpcIds;
    private HashSet<int>? _worldIds;

    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);

    /// <summary>End (turn-in) NPC ids; falls back to <see cref="StartNpcIds"/> when not declared (Java parity).</summary>
    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= QuestScriptIds.Parse(EndNpcIdsRaw) is { Count: > 0 } explicitEnds
        ? explicitEnds
        : new HashSet<int>(StartNpcIds);

    [XmlIgnore]
    public HashSet<int> WorldIds => _worldIds ??= QuestScriptIds.Parse(WorldIdsRaw);
}

/// <summary>
/// Port of Java <c>KillSpawnedData</c> (questEngine.handlers.models) — talk to a spawner object to
/// spawn a mob, then kill it. Extends Java's MonsterHuntData in the XSD, but KillSpawned.java's own
/// handler only ever reads start/end npcs and the spawned_monster list, so only those are modeled.
/// </summary>
public sealed class KillSpawnedScriptEntry
{
    [XmlAttribute("id")]            public int    Id             { get; set; }
    [XmlAttribute("start_npc_ids")] public string StartNpcIdsRaw  { get; set; } = "0";
    [XmlAttribute("end_npc_ids")]   public string EndNpcIdsRaw    { get; set; } = "0";

    [XmlElement("spawned_monster")] public List<SpawnedMonsterEntry> SpawnedMonsters { get; set; } = new();

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _endNpcIds;

    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);

    /// <summary>End (turn-in) NPC ids; falls back to <see cref="StartNpcIds"/> when not declared (Java parity).</summary>
    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= QuestScriptIds.Parse(EndNpcIdsRaw) is { Count: > 0 } explicitEnds
        ? explicitEnds
        : new HashSet<int>(StartNpcIds);
}

/// <summary>One spawner-object -> spawned-mob kill-goal pair within a &lt;kill_spawned&gt; entry (Java <c>SpawnedMonster</c>).</summary>
public sealed class SpawnedMonsterEntry
{
    [XmlAttribute("var")]            public int    Var           { get; set; }
    [XmlAttribute("end_var")]        public int    EndVar        { get; set; }
    [XmlAttribute("npc_ids")]        public string NpcIdsRaw      { get; set; } = "0";
    [XmlAttribute("spawner_object")] public int    SpawnerObject  { get; set; }

    private HashSet<int>? _npcIds;
    [XmlIgnore]
    public HashSet<int> NpcIds => _npcIds ??= QuestScriptIds.Parse(NpcIdsRaw);
}

/// <summary>
/// Port of Java <c>WorkOrdersData</c> (questEngine.handlers.models) — crafting "work order" quests:
/// accept teaches the recipe and gives its components, turn-in consumes the crafted product
/// (quest_data.xml's collect_items). Lives in quest_script_data/work_order.xml, not quest_data/.
/// </summary>
public sealed class WorkOrderScriptEntry
{
    [XmlAttribute("id")]            public int    Id             { get; set; }
    [XmlAttribute("start_npc_ids")] public string StartNpcIdsRaw  { get; set; } = "0";
    [XmlAttribute("recipe_id")]     public int    RecipeId        { get; set; }

    [XmlElement("give_component")] public List<CollectItem> GiveComponents { get; set; } = new();

    private HashSet<int>? _startNpcIds;
    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);
}

/// <summary>
/// Port of Java <c>CraftingRewardsData</c> (questEngine.handlers.models) — accept-a-recipe-then-
/// craft-to-learn-the-skill quests (e.g. "[Expert] Weaponsmithing Expert"). 28 entries on disk, all
/// with a non-zero <c>movie</c> attribute. Attribute names verified against the actual XML/XSD.
/// </summary>
public sealed class CraftingRewardsScriptEntry
{
    [XmlAttribute("id")]            public int Id           { get; set; }
    [XmlAttribute("start_npc_id")]  public int StartNpcId    { get; set; }
    [XmlAttribute("end_npc_id")]    public int EndNpcId      { get; set; }
    [XmlAttribute("skill_id")]      public int SkillId       { get; set; }
    [XmlAttribute("level_reward")]  public int LevelReward   { get; set; }
    // Parsed for data completeness only — no SM_MOVIE/OnMovieEnd hook exists yet in this port (see
    // CraftingRewardsHandler remarks); the skill is granted unconditionally on reward click instead.
    [XmlAttribute("movie")]         public int Movie         { get; set; }
}

/// <summary>
/// Port of Java <c>RelicRewardsData</c> (questEngine.handlers.models) — "turn in one of 4 relic
/// item types for an AP reward" exchange quests. 30 entries on disk.
/// </summary>
public sealed class RelicRewardsScriptEntry
{
    [XmlAttribute("id")]            public int    Id             { get; set; }
    [XmlAttribute("start_npc_ids")] public string StartNpcIdsRaw  { get; set; } = "0";
    [XmlAttribute("relic_var1")]    public int    RelicVar1       { get; set; }
    [XmlAttribute("relic_var2")]    public int    RelicVar2       { get; set; }
    [XmlAttribute("relic_var3")]    public int    RelicVar3       { get; set; }
    [XmlAttribute("relic_var4")]    public int    RelicVar4       { get; set; }
    [XmlAttribute("relic_count")]   public int    RelicCount      { get; set; }

    private HashSet<int>? _startNpcIds;
    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);
}

/// <summary>
/// Port of Java <c>FountainRewardsData</c> (questEngine.handlers.models) — "coin fountain" exchange
/// quests: insert a coin item (quest_data.xml's &lt;inventory_items&gt;) for a fixed reward
/// (usually exp). 7 entries on disk.
/// </summary>
public sealed class FountainRewardsScriptEntry
{
    [XmlAttribute("id")]            public int    Id             { get; set; }
    [XmlAttribute("start_npc_ids")] public string StartNpcIdsRaw  { get; set; } = "0";

    private HashSet<int>? _startNpcIds;
    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);
}

/// <summary>
/// Port of Java <c>SkillUseData</c> (questEngine.handlers.models) — "use skill X N times" quests.
/// Each &lt;skill&gt; child is an independent counter group (own var span), mirroring
/// &lt;monster_hunt&gt;'s &lt;monster&gt; groups. 30 entries on disk.
/// </summary>
public sealed class SkillUseScriptEntry
{
    [XmlAttribute("id")]           public int Id          { get; set; }
    [XmlAttribute("start_npc_id")] public int StartNpcId   { get; set; }
    [XmlAttribute("end_npc_id")]   public int EndNpcId     { get; set; }

    [XmlElement("skill")] public List<SkillUseGroupEntry> Skills { get; set; } = new();
}

/// <summary>One skill-use-count objective within a &lt;skill_use&gt; entry (Java <c>QuestSkillData</c>).</summary>
public sealed class SkillUseGroupEntry
{
    [XmlAttribute("ids")]       public string SkillIdsRaw { get; set; } = "0";
    [XmlAttribute("start_var")] public int    StartVar     { get; set; } // parsed for parity; unused (matches MonsterEntry.StartVar precedent)
    [XmlAttribute("end_var")]   public int    EndVar       { get; set; }
    [XmlAttribute("var_num")]   public int    VarNum       { get; set; }

    private HashSet<int>? _skillIds;
    [XmlIgnore]
    public HashSet<int> SkillIds => _skillIds ??= QuestScriptIds.Parse(SkillIdsRaw);
}

/// <summary>
/// Port of Java <c>MentorMonsterHuntData</c> (questEngine.handlers.models, extends
/// <c>MonsterHuntData</c>) — a kill-N-monsters hunt intended to be shared between an active mentor
/// and their mentee (see <see cref="AionLightning.Game.QuestEngine.Handlers.Templates.MentorMonsterHuntHandler"/>
/// remarks for why the mentor/mentee relation gate isn't enforced by this port). Same
/// &lt;monster var/end_var/npc_ids&gt; shape as &lt;monster_hunt&gt;, reusing <see cref="MonsterEntry"/>.
/// </summary>
public sealed class MentorMonsterHuntScriptEntry
{
    [XmlAttribute("id")]              public int    Id              { get; set; }
    [XmlAttribute("start_npc_ids")]   public string StartNpcIdsRaw   { get; set; } = "0";
    [XmlAttribute("end_npc_ids")]     public string EndNpcIdsRaw     { get; set; } = "0";
    [XmlAttribute("min_mente_level")] public int    MinMenteLevel    { get; set; } = 1;
    [XmlAttribute("max_mente_level")] public int    MaxMenteLevel    { get; set; } = 99;

    [XmlElement("monster")] public List<MonsterEntry> Monsters { get; set; } = new();

    private HashSet<int>? _startNpcIds;
    private HashSet<int>? _endNpcIds;

    [XmlIgnore]
    public HashSet<int> StartNpcIds => _startNpcIds ??= QuestScriptIds.Parse(StartNpcIdsRaw);

    /// <summary>End (turn-in) NPC ids; falls back to <see cref="StartNpcIds"/> when not declared (Java parity).</summary>
    [XmlIgnore]
    public HashSet<int> EndNpcIds => _endNpcIds ??= QuestScriptIds.Parse(EndNpcIdsRaw) is { Count: > 0 } explicitEnds
        ? explicitEnds
        : new HashSet<int>(StartNpcIds);
}
