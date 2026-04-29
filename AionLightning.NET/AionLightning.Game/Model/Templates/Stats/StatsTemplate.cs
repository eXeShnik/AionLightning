using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Stats;

public class StatsTemplate
{
    [XmlAttribute("maxHp")]               public int MaxHp              { get; set; }
    [XmlAttribute("maxMp")]               public int MaxMp              { get; set; }
    [XmlAttribute("evasion")]             public int Evasion            { get; set; }
    [XmlAttribute("block")]               public int Block              { get; set; }
    [XmlAttribute("parry")]               public int Parry              { get; set; }
    [XmlAttribute("main_hand_attack")]    public int MainHandAttack     { get; set; }
    [XmlAttribute("main_hand_accuracy")]  public int MainHandAccuracy   { get; set; }
    [XmlAttribute("main_hand_crit_rate")] public int MainHandCritRate   { get; set; }
    [XmlAttribute("magic_accuracy")]      public int MagicAccuracy      { get; set; }
    [XmlElement("speeds")]                public CreatureSpeeds? Speeds { get; set; }

    public float WalkSpeed => Speeds?.Walk ?? 1.5f;
    public float RunSpeed  => Speeds?.Run  ?? 6.0f;
    public float FlySpeed  => Speeds?.Fly  ?? 9.0f;
}
