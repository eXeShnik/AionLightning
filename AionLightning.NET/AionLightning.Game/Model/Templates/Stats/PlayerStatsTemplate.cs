using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Stats;

public class PlayerStatsTemplate : StatsTemplate
{
    [XmlAttribute("power")]     public int Power    { get; set; }
    [XmlAttribute("health")]    public int Health   { get; set; }
    [XmlAttribute("agility")]   public int Agility  { get; set; }
    [XmlAttribute("accuracy")]  public int Accuracy { get; set; }
    [XmlAttribute("knowledge")] public int Knowledge { get; set; }
    [XmlAttribute("will")]      public int Will     { get; set; }
}
