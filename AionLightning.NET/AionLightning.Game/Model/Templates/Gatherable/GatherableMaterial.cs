using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Gatherable;

[XmlRoot("material")]
public sealed class GatherableMaterial
{
    [XmlAttribute("itemid")] public int  ItemId { get; set; }
    [XmlAttribute("nameid")] public int  NameId { get; set; }
    [XmlAttribute("rate")]   public int  Rate   { get; set; } // out of 10_000_000
    [XmlAttribute("name")]   public string Name { get; set; } = "";
}
