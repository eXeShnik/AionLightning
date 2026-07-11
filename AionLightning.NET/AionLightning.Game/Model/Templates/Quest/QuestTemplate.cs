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
    [XmlElement("rewards")]              public QuestRewards?          Rewards        { get; set; }
    [XmlElement("quest_kill")]           public List<QuestKill>        QuestKills     { get; set; } = new();
    // Leftover crafting components (WorkOrders template) to strip from the player's bag on
    // completion — distinct from CollectItems, which holds the crafted product being turned in.
    [XmlElement("quest_work_items")]     public QuestWorkItemsHolder?  QuestWorkItems { get; set; }
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
