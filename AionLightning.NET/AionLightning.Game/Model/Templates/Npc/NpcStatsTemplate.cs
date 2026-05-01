using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Stats;

namespace AionLightning.Game.Model.Templates.Npc;

public sealed class NpcStatsTemplate
{
    [XmlAttribute("maxHp")]              public int  MaxHp             { get; set; }
    [XmlAttribute("maxXp")]              public long MaxXp             { get; set; }
    [XmlAttribute("main_hand_attack")]   public int  MainHandAttack    { get; set; }
    [XmlAttribute("main_hand_accuracy")] public int MainHandAccuracy  { get; set; }
    [XmlAttribute("pdef")]               public int PDef              { get; set; }
    [XmlAttribute("mresist")]            public int MResist           { get; set; }
    [XmlAttribute("evasion")]            public int Evasion           { get; set; }
    [XmlAttribute("power")]              public int Power             { get; set; }
    [XmlAttribute("accuracy")]           public int Accuracy          { get; set; }

    [XmlElement("speeds")]
    public CreatureSpeeds? Speeds { get; set; }

    public float RunSpeed => Speeds?.Run ?? 6.0f;
}
