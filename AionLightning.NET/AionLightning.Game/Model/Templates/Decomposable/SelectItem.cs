using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Decomposable;

[XmlType("SelectItem")]
public sealed class SelectItem
{
    [XmlAttribute("id")]    public int Id    { get; set; }
    [XmlAttribute("count")] public int Count { get; set; } = 1;
}
