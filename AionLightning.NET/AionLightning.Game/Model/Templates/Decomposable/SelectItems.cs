using System.Xml.Serialization;
using AionLightning.Game.Model;

namespace AionLightning.Game.Model.Templates.Decomposable;

[XmlType("SelectItems")]
public sealed class SelectItems
{
    [XmlAttribute("player_class")] public PlayerClass PlayerClass { get; set; } = PlayerClass.ALL;
    [XmlElement("item")]           public List<SelectItem> Items  { get; set; } = new();
}
