using System.Xml.Serialization;
using AionLightning.Game.Model;

namespace AionLightning.Game.Model.Templates.Skill;

public sealed class SkillLearnTemplate
{
    [XmlAttribute("skillId")]    public int         SkillId    { get; set; }
    [XmlAttribute("skillLevel")] public int         SkillLevel { get; set; }
    [XmlAttribute("classId")]    public PlayerClass ClassId    { get; set; } = PlayerClass.ALL;
    [XmlAttribute("race")]       public Race        Race       { get; set; } = Race.PC_ALL;
    [XmlAttribute("minLevel")]   public int         MinLevel   { get; set; }
    [XmlAttribute("autolearn")]  public bool        AutoLearn  { get; set; }
    [XmlAttribute("stigma")]     public bool        Stigma     { get; set; }
    [XmlAttribute("name")]       public string      Name       { get; set; } = "";
}
