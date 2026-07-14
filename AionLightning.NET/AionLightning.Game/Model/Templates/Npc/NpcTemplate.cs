using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Npc;

[XmlRoot("npc_template")]
public sealed class NpcTemplate
{
    [XmlAttribute("npc_id")]   public int    NpcId       { get; set; }
    [XmlAttribute("name")]     public string Name        { get; set; } = string.Empty;
    [XmlAttribute("name_id")]  public int    NameId      { get; set; }
    [XmlAttribute("title_id")] public int    TitleId     { get; set; }
    [XmlAttribute("level")]    public byte   Level       { get; set; }
    [XmlAttribute("height")]   public float  Height      { get; set; } = 1f;
    [XmlAttribute("srange")]   public int    AggroRange  { get; set; }
    [XmlAttribute("adelay")]   public int    AttackDelay { get; set; } = 1500;
    [XmlAttribute("ai")]       public string Ai          { get; set; } = "dummy";
    [XmlAttribute("type")]     public string NpcType     { get; set; } = "GENERAL";
    [XmlAttribute("tribe")]    public string Tribe       { get; set; } = "GENERAL";
    [XmlAttribute("rank")]     public string Rank        { get; set; } = "NOVICE";
    [XmlAttribute("rating")]   public string Rating      { get; set; } = "NORMAL";
    [XmlAttribute("race")]     public string NpcRace     { get; set; } = "GENERAL";

    [XmlElement("stats")]
    public NpcStatsTemplate? Stats { get; set; }

    [XmlElement("bound_radius")]
    public NpcBoundRadius? BoundRadius { get; set; }

    /// <summary>Maps to &lt;kisk_stats usemask=".." members=".." resurrects=".."/&gt; — present only on
    /// kisk (bindstone) NPC templates (npc_templates.xml ai="kisk"/"invisiblekisk"). Null for every
    /// other NPC. See <see cref="Model.GameObjects.Kisk"/>.</summary>
    [XmlElement("kisk_stats")]
    public KiskStatsTemplate? KiskStats { get; set; }

    public int MaxHp => Stats?.MaxHp ?? 100;
}

public sealed class NpcBoundRadius
{
    [XmlAttribute("front")] public float Front { get; set; } = 1.0f;
    [XmlAttribute("side")]  public float Side  { get; set; } = 1.0f;
    [XmlAttribute("upper")] public float Upper { get; set; } = 1.0f;
}

/// <summary>Mirrors Java KiskStatsTemplate — who may bind (useMask: 0/1=race, 2=legion, 3=solo,
/// 4=group, 5=alliance), how many players may be bound at once, and how many total resurrects the
/// kisk grants before it self-despawns. Defaults match the Java XML-binding defaults.</summary>
public sealed class KiskStatsTemplate
{
    [XmlAttribute("usemask")]    public int UseMask       { get; set; } = 4;
    [XmlAttribute("members")]    public int MaxMembers     { get; set; } = 6;
    [XmlAttribute("resurrects")] public int MaxResurrects  { get; set; } = 18;
}
