using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Quest.Script;

/// <summary>
/// Root of a <c>quest_script_data/*.xml</c> file. Java's XSD interleaves many sibling element
/// types (report_to, monster_hunt, ...); Phase 1 only maps &lt;item_collecting&gt; — other
/// elements are simply skipped by <see cref="XmlSerializer"/>.
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
