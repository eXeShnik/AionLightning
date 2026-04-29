using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Stats;

public sealed class CreatureSpeeds
{
    [XmlAttribute("walk")] public float Walk { get; set; }
    [XmlAttribute("run")]  public float Run  { get; set; }
    [XmlAttribute("fly")]  public float Fly  { get; set; }
}
