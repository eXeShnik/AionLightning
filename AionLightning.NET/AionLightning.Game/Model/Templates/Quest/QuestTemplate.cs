using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Quest;

[XmlRoot("quest")]
public sealed class QuestTemplate
{
    [XmlAttribute("id")]                 public int    Id           { get; set; }
    [XmlAttribute("name")]               public string Name         { get; set; } = string.Empty;
    [XmlAttribute("minlevel_permitted")] public byte   MinLevel     { get; set; } = 1;
    [XmlAttribute("race_permitted")]     public string Race         { get; set; } = "PC_ALL";
    [XmlAttribute("cannot_share")]       public bool   CannotShare  { get; set; } = false;

    [XmlElement("collect_items")]        public CollectItemsHolder?    CollectItems   { get; set; }
    // quest_data.xsd allows multiple sibling <rewards> blocks per quest (maxOccurs="unbounded") —
    // used by multi-tier exchange quests like relic_rewards (e.g. id 21281 has 4 <rewards
    // reward_abyss_point="..."/> blocks, one per relic type). Bound as a list so a rewardIndex can
    // select a specific tier (Java QuestService.finishQuest: `template.getRewards().get(reward)`).
    [XmlElement("rewards")]              public List<QuestRewards>     RewardsList    { get; set; } = new();
    [XmlElement("quest_kill")]           public List<QuestKill>        QuestKills     { get; set; } = new();
    // Leftover crafting components (WorkOrders template) to strip from the player's bag on
    // completion — distinct from CollectItems, which holds the crafted product being turned in.
    [XmlElement("quest_work_items")]     public QuestWorkItemsHolder?  QuestWorkItems { get; set; }
    // "Coin fountain"-style presence/consume gate (Java InventoryItems, distinct from CollectItems)
    // — e.g. quest 1717 requires + consumes one 186000031 coin item, with no <collect_items> at all.
    [XmlElement("inventory_items")]      public InventoryItemsHolder?  InventoryItems { get; set; }
    // Event/lunar/movie-style bonus gate (Java QuestBonuses, quest_data.xsd maxOccurs="unbounded" but
    // only the first entry is ever read — Java QuestService.getRewardItems: `bonuses.get(0)`).
    [XmlElement("bonus")]                 public List<QuestBonus>      BonusList      { get; set; } = new();

    /// <summary>
    /// The quest's single reward block, or the last-declared one when <see cref="RewardsList"/> has
    /// more than one (matches prior behavior: XmlSerializer only ever kept the last of several
    /// same-named elements bound to a singular property). New code that needs to pick a specific
    /// tier by index (multi-`&lt;rewards&gt;` quests) should read <see cref="RewardsList"/> directly.
    /// </summary>
    [XmlIgnore]
    public QuestRewards? Rewards => RewardsList.Count > 0 ? RewardsList[^1] : null;
}

public sealed class InventoryItemsHolder
{
    [XmlElement("inventory_item")]
    public List<CollectItem> Items { get; set; } = new();
}

public sealed class QuestWorkItemsHolder
{
    [XmlElement("quest_work_item")]
    public List<CollectItem> Items { get; set; } = new();
}

public sealed class CollectItemsHolder
{
    [XmlElement("collect_item")]
    public List<CollectItem> Items { get; set; } = new();
}

public sealed class CollectItem
{
    [XmlAttribute("item_id")] public int  ItemId { get; set; }
    [XmlAttribute("count")]   public long Count  { get; set; } = 1;
}

public sealed class QuestRewards
{
    [XmlAttribute("exp")]                public long Exp              { get; set; }
    [XmlAttribute("title")]              public int  Title            { get; set; } = -1;
    [XmlAttribute("gold")]               public long Gold             { get; set; }
    [XmlAttribute("reward_abyss_point")] public int  RewardAbyssPoint { get; set; }

    [XmlElement("selectable_reward_item")]
    public List<SelectableRewardItem> SelectableItems { get; set; } = new();

    [XmlElement("reward_item")]
    public List<RewardItem> RewardItems { get; set; } = new();
}

public sealed class SelectableRewardItem
{
    [XmlAttribute("item_id")] public int  ItemId { get; set; }
    [XmlAttribute("count")]   public long Count  { get; set; } = 1;
}

public sealed class RewardItem
{
    [XmlAttribute("item_id")] public int  ItemId { get; set; }
    [XmlAttribute("count")]   public long Count  { get; set; } = 1;
}

public sealed class QuestKill
{
    [XmlAttribute("npc_ids")] public string NpcIdsRaw { get; set; } = string.Empty;
    [XmlAttribute("seq")]     public int    Seq        { get; set; }
    [XmlAttribute("count")]   public int    Count      { get; set; } = 1;

    private HashSet<int>? _npcIds;
    public HashSet<int> NpcIds => _npcIds ??= new HashSet<int>(
        NpcIdsRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
}

/// <summary>Maps to &lt;bonus type="..." level="N" skill="N"/&gt; (Java QuestBonuses/BonusType). Type is
/// kept as the raw XML string (e.g. "MOVIE", "LUNAR", "TASK") to match <see cref="QuestEngine.QuestEngine.RegisterOnBonusApply"/>'s
/// string-keyed index instead of introducing a parallel BonusType enum.</summary>
public sealed class QuestBonus
{
    [XmlAttribute("type")]  public string Type  { get; set; } = "NONE";
    [XmlAttribute("level")] public int    Level { get; set; }
    [XmlAttribute("skill")] public int    Skill { get; set; }
}
