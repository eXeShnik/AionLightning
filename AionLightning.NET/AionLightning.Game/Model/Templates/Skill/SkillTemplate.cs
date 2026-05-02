using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model;

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
    [XmlAttribute("tslot")]      public string    TSlot      { get; set; } = "";

    [XmlElement("properties")]   public SkillProperties? Properties { get; set; }
    [XmlElement("effects")]      public SkillEffects?    Effects    { get; set; }

    public AbnormalCcFlags CcFlags => Effects?.CcFlags ?? AbnormalCcFlags.None;

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

    /// <summary>True when the skill is an AoE centered on the caster (first_target=ME, target_type=AREA).</summary>
    public bool IsCasterAoe =>
        string.Equals(Properties?.FirstTarget, "ME", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Properties?.TargetType, "AREA", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the skill is an AoE centered on the selected target (first_target=TARGET, target_type=AREA).</summary>
    public bool IsTargetAoe =>
        string.Equals(Properties?.FirstTarget, "TARGET", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Properties?.TargetType, "AREA", StringComparison.OrdinalIgnoreCase);

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

/// <summary>Instant heal descriptor parsed from &lt;healinstant&gt; and &lt;mphealinstant&gt; effect elements.</summary>
public readonly record struct SkillHealInfo(
    int    BaseValue,  // flat heal value (or percent of max if IsPercent)
    int    Delta,      // per-level increase
    bool   IsPercent,  // value is % of max HP/MP
    string HealType    // "hp" or "mp"
);

/// <summary>Per-tick HoT descriptor parsed from &lt;heal&gt; and &lt;mpheal&gt; effect elements.</summary>
public readonly record struct SkillHotInfo(
    int    CheckTimeMs,
    int    BaseValue,
    int    Delta,
    int    Duration2Ms,
    string HealType    // "hp" or "mp"
);

/// <summary>Per-tick DoT descriptor parsed from &lt;bleed&gt;, &lt;poison&gt;, &lt;disease&gt; effect elements.</summary>
public readonly record struct SkillDotInfo(
    int    CheckTimeMs,   // tick interval in ms
    int    BaseValue,     // damage per tick at skill level 1 (value + delta * level applied at cast time)
    int    Delta,         // per-level damage increase
    int    Duration2Ms,   // total effect duration in ms
    string DotType,       // "bleed" | "poison" | "disease"
    string Element        // "FIRE", "EARTH", etc. (reserved for future element resist)
);

/// <summary>Captures CC and DoT effect elements from the &lt;effects&gt; block of a skill_template.</summary>
public sealed class SkillEffects
{
    [XmlAnyElement]
    public XmlElement[]? Elements { get; set; }

    public AbnormalCcFlags CcFlags =>
        Elements?.Aggregate(AbnormalCcFlags.None, (acc, e) => acc | ElementToCcFlag(e.LocalName))
        ?? AbnormalCcFlags.None;

    public bool HasDispelDebuff => Elements?.Any(e => e.LocalName == "dispeldebuff") == true;
    public bool HasDispelBuff   => Elements?.Any(e => e.LocalName == "dispelbuff")   == true;

    private static readonly HashSet<string> SnareNames        = ["snare", "absolutesnare"];

    /// <summary>
    /// Movement speed percent change from the first snare/absolutesnare effect with a PERCENT SPEED change.
    /// Typically -50 (halve speed). 0 means no movement speed change.
    /// </summary>
    public int SnareSpeedPct
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (!SnareNames.Contains(e.LocalName)) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "SPEED",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int pct)) return pct;
                }
            }
            return 0;
        }
    }

    private static readonly HashSet<string> HealInstantNames  = ["healinstant", "mphealinstant"];
    private static readonly HashSet<string> HotNames          = ["heal", "mpheal"];
    private static readonly HashSet<string> DotNames          = ["bleed", "poison", "disease"];
    // Elements that carry debuff durations via their duration2 attribute
    private static readonly HashSet<string> EffectDurNames    = ["slow", "snare", "absolutesnare", "statdown", "statup", "blind", "confuse", "absoluteslow"];

    public IReadOnlyList<SkillHealInfo> HealEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillHealInfo>();
            foreach (var e in Elements)
            {
                if (!HealInstantNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                string ht = e.LocalName == "healinstant" ? "hp" : "mp";
                list.Add(new(val, dlt, pct, ht));
            }
            return list;
        }
    }

    /// <summary>
    /// Max duration2 found in any debuff-class effect element (slow/snare/statdown/statup/blind/confuse).
    /// Used when the skill_template's own duration attribute is 0.
    /// </summary>
    public int EffectDuration
    {
        get
        {
            if (Elements is null) return 0;
            int max = 0;
            foreach (var e in Elements)
            {
                if (!EffectDurNames.Contains(e.LocalName)) continue;
                if (int.TryParse(e.GetAttribute("duration2"), out int d) && d > max)
                    max = d;
            }
            return max;
        }
    }

    public IReadOnlyList<SkillHotInfo> HotEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillHotInfo>();
            foreach (var e in Elements)
            {
                if (!HotNames.Contains(e.LocalName)) continue;
                if (!int.TryParse(e.GetAttribute("checktime"), out int check) || check <= 0) continue;
                if (!int.TryParse(e.GetAttribute("duration2"), out int dur)   || dur   <= 0) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                string ht = e.LocalName == "heal" ? "hp" : "mp";
                list.Add(new(check, val, dlt, dur, ht));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillDotInfo> DotEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillDotInfo>();
            foreach (var e in Elements)
            {
                if (!DotNames.Contains(e.LocalName)) continue;
                if (!int.TryParse(e.GetAttribute("checktime"), out int check) || check <= 0) continue;
                if (!int.TryParse(e.GetAttribute("duration2"), out int dur)   || dur   <= 0) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                list.Add(new(check, val, dlt, dur, e.LocalName, e.GetAttribute("element") ?? string.Empty));
            }
            return list;
        }
    }

    private static AbnormalCcFlags ElementToCcFlag(string name) => name switch
    {
        "stun" or "stunalways"          => AbnormalCcFlags.Stun,
        "sleep"                         => AbnormalCcFlags.Sleep,
        "root"                          => AbnormalCcFlags.Root,
        "silence"                       => AbnormalCcFlags.Silence,
        "bind"                          => AbnormalCcFlags.Sleep,  // BIND shares Sleep semantics for cant-attack
        "paralyze"                      => AbnormalCcFlags.Paralyze,
        "fear"                          => AbnormalCcFlags.Fear,
        "stagger" or "staggeralways"    => AbnormalCcFlags.Stagger,
        "stumble" or "stumblealways"    => AbnormalCcFlags.Stumble,
        "spin"                          => AbnormalCcFlags.Spin,
        _                               => AbnormalCcFlags.None
    };
}
