using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Npc;

public sealed class NpcStatsTemplate
{
    [XmlAttribute("maxHp")]              public int MaxHp             { get; set; }
    [XmlAttribute("main_hand_attack")]   public int MainHandAttack    { get; set; }
    [XmlAttribute("main_hand_accuracy")] public int MainHandAccuracy  { get; set; }
    [XmlAttribute("pdef")]               public int PDef              { get; set; }
    [XmlAttribute("evasion")]            public int Evasion           { get; set; }
    [XmlAttribute("power")]              public int Power             { get; set; }
    [XmlAttribute("accuracy")]           public int Accuracy          { get; set; }
}
