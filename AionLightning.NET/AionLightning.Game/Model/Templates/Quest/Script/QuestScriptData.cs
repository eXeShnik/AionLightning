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

    /// <summary>Start NPC template ids (0 = placeholder, stripped — matches Java's <c>startNpcs.remove(0)</c>).</summary>
    public HashSet<int> StartNpcIds => _startNpcIds ??= ParseIds(StartNpcIdsRaw);

    /// <summary>Action item ids (npcs the player interacts with mid-quest); empty when not declared.</summary>
    public HashSet<int> ActionItemIds => _actionItemIds ??= ParseIds(ActionItemIdsRaw);

    /// <summary>End (turn-in) NPC ids; falls back to <see cref="StartNpcIds"/> when not declared (Java parity).</summary>
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
