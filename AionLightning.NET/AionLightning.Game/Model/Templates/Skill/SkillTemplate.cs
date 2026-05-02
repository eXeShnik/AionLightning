using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Skill;

public sealed class SkillTemplate
{
    [XmlAttribute("skill_id")]   public int       SkillId    { get; set; }
    [XmlAttribute("name")]       public string    Name       { get; set; } = "";
    [XmlAttribute("nameId")]     public int       NameId     { get; set; }
    [XmlAttribute("cooldownId")] public int       CooldownId { get; set; }
    [XmlAttribute("stack")]      public string    Stack      { get; set; } = "NONE";
    [XmlAttribute("lvl")]        public int       Level      { get; set; }
    [XmlAttribute("skilltype")]    public SkillType    SkillType  { get; set; }
    [XmlAttribute("skillsubtype")] public SkillSubType SubType    { get; set; }
    [XmlAttribute("activation")]   public string       Activation { get; set; } = "";
    [XmlAttribute("cooldown")]   public int       Cooldown   { get; set; }
    [XmlAttribute("duration")]   public int       Duration   { get; set; }

    [XmlElement("properties")]   public SkillProperties? Properties { get; set; }

    public float  CastRange       => Properties?.CastRange       ?? 0f;
    public string TargetType      => Properties?.TargetType      ?? string.Empty;
    public string TargetRelation  => Properties?.TargetRelation  ?? string.Empty;
    public float  EffectiveRange  => Properties?.EffectiveRange  ?? 0f;
    public float  EffectiveAltitude => Properties?.EffectiveAltitude ?? 0f;
    public int    TargetMaxCount  => Properties?.TargetMaxCount is > 0 ? Properties.TargetMaxCount : 6;

    /// <summary>True when the skill hits an area around a ground point (client sends targetType 1).</summary>
    public bool IsGroundAoe =>
        string.Equals(Properties?.FirstTarget, "POINT", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Properties?.TargetType, "AREA",  StringComparison.OrdinalIgnoreCase);

    // When cooldownId == 0 in XML, each skill acts as its own cooldown group (mirrors Java getCooldownId())
    public int EffectiveCooldownId => CooldownId > 0 ? CooldownId : SkillId;
}

public sealed class SkillProperties
{
    [XmlAttribute("first_target_range")] public float  CastRange      { get; set; }
    [XmlAttribute("first_target")]       public string FirstTarget     { get; set; } = string.Empty;
    [XmlAttribute("target_type")]        public string TargetType      { get; set; } = string.Empty;
    [XmlAttribute("target_relation")]    public string TargetRelation  { get; set; } = string.Empty;
    [XmlAttribute("effective_range")]    public float  EffectiveRange  { get; set; }
    [XmlAttribute("effective_altitude")] public float  EffectiveAltitude { get; set; }
    [XmlAttribute("target_maxcount")]    public int    TargetMaxCount  { get; set; }
}
