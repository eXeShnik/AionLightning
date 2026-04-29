using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Npc;

[XmlRoot("npc_template")]
public sealed class NpcTemplate
{
    [XmlAttribute("npc_id")]  public int    NpcId      { get; set; }
    [XmlAttribute("name")]    public string Name       { get; set; } = string.Empty;
    [XmlAttribute("name_id")] public int    NameId     { get; set; }
    [XmlAttribute("level")]   public byte   Level      { get; set; }
    [XmlAttribute("height")]  public float  Height     { get; set; } = 1f;
    [XmlAttribute("srange")]  public int    AggroRange { get; set; }
    [XmlAttribute("ai")]      public string Ai         { get; set; } = "dummy";

    [XmlElement("stats")]
    public NpcStatsTemplate? Stats { get; set; }

    public int MaxHp => Stats?.MaxHp ?? 100;
}
