using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Npc;

[XmlRoot("npc_template")]
public sealed class NpcTemplate
{
    [XmlAttribute("npc_id")]   public int    NpcId      { get; set; }
    [XmlAttribute("name")]     public string Name       { get; set; } = string.Empty;
    [XmlAttribute("name_id")]  public int    NameId     { get; set; }
    [XmlAttribute("title_id")] public int    TitleId    { get; set; }
    [XmlAttribute("level")]    public byte   Level      { get; set; }
    [XmlAttribute("height")]   public float  Height     { get; set; } = 1f;
    [XmlAttribute("srange")]   public int    AggroRange { get; set; }
    [XmlAttribute("adelay")]   public int    AttackDelay { get; set; } = 1500;
    [XmlAttribute("ai")]       public string Ai         { get; set; } = "dummy";
    [XmlAttribute("type")]     public string NpcType    { get; set; } = "GENERAL";

    [XmlElement("stats")]
    public NpcStatsTemplate? Stats { get; set; }

    [XmlElement("bound_radius")]
    public NpcBoundRadius? BoundRadius { get; set; }

    public int MaxHp => Stats?.MaxHp ?? 100;
}

public sealed class NpcBoundRadius
{
    [XmlAttribute("front")] public float Front { get; set; } = 1.0f;
    [XmlAttribute("side")]  public float Side  { get; set; } = 1.0f;
    [XmlAttribute("upper")] public float Upper { get; set; } = 1.0f;
}
