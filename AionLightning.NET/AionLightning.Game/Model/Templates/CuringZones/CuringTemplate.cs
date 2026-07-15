using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.CuringZones;

/// <summary>Java model.templates.curingzones.CuringTemplate — a single curing-point spot (map id +
/// coordinates + effect range) parsed from curing_objects.xml.</summary>
public sealed class CuringTemplate
{
    [XmlAttribute("map_id")] public int MapId { get; set; }
    [XmlAttribute("x")] public float X { get; set; }
    [XmlAttribute("y")] public float Y { get; set; }
    [XmlAttribute("z")] public float Z { get; set; }
    [XmlAttribute("range")] public float Range { get; set; }
}
