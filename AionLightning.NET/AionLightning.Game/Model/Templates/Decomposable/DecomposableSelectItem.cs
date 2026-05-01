using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Decomposable;

[XmlType("DecomposableSelectItem")]
public sealed class DecomposableSelectItem
{
    [XmlAttribute("item_id")] public int ItemId { get; set; }
    [XmlElement("items")]     public List<SelectItems> SelectItems { get; set; } = new();
}
