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

/// <summary>Per-tick DoT descriptor parsed from &lt;bleed&gt;, &lt;poison&gt;, &lt;disease&gt;, &lt;spellatk&gt;, &lt;spellatkdrain&gt; effect elements.</summary>
public readonly record struct SkillDotInfo(
    int    CheckTimeMs,   // tick interval in ms
    int    BaseValue,     // damage per tick at skill level 1 (value + delta * level applied at cast time)
    int    Delta,         // per-level damage increase
    int    Duration2Ms,   // total effect duration in ms
    string DotType,       // "bleed" | "poison" | "disease" | "spellatk" | "spellatkdrain"
    string Element,       // "FIRE", "EARTH", etc. (reserved for future element resist)
    int    HpPercent,     // drain HP per tick: tickDamage * HpPercent / 100 (0 = no drain)
    int    MpPercent      // drain MP per tick: tickDamage * MpPercent / 100 (0 = no drain)
);

/// <summary>Skill direct-damage descriptor parsed from &lt;skillatk&gt;/&lt;spellatkinstant&gt; (regular), &lt;skillatkdraininstant&gt;/&lt;spellatkdraininstant&gt; (drain), and &lt;procatk_instant&gt; (proc).</summary>
public readonly record struct SkillDamageInfo(
    int    BaseValue,    // damage at skill level 1
    int    Delta,        // per-level damage increase
    string DamageType,   // "physical" | "magical"
    string Element,      // elemental type (e.g. "FIRE", "EARTH") — reserved for future elemental resist
    int    AccuracyMod,  // accmod2 value (magic accuracy modifier, 0 = none)
    int    HpPercent,    // drain HP gained: dealtDamage * HpPercent / 100 (0 = no HP drain)
    int    MpPercent,    // drain MP gained: dealtDamage * MpPercent / 100 (0 = no MP drain)
    string Variant,      // raw effect element name (e.g. "skillatk", "procatk_instant") — used to pick combat-log LogId
    bool   IsNoResist    // noresist="true" — skip magic resist / physical dodge roll for this hit
);

/// <summary>Target-MP burn descriptor parsed from &lt;mpattackinstant&gt; (Java MpAttackInstantEffect).</summary>
public readonly record struct SkillMpAttackInfo(
    int    BaseValue,    // mp burn at skill level 1 (flat MP, or % of target MaxMp when IsPercent)
    int    Delta,        // per-level scaling
    bool   IsPercent,    // true = value is percent of target MaxMp
    string Element       // elemental flavor (reserved)
);

/// <summary>Defense-bypass damage parsed from &lt;noreducespellatk&gt; (Java NoReduceSpellATKInstantEffect).</summary>
public readonly record struct SkillNoReduceInfo(
    int    BaseValue,    // damage at skill level 1 (flat or % of target MaxHp when IsPercent)
    int    Delta,        // per-level scaling
    bool   IsPercent,    // true = value is percent of target MaxHp
    string Element       // elemental flavor (reserved)
);

/// <summary>"Heal caster on attack" descriptor parsed from &lt;healcastoronatk&gt; (Java HealCastorOnAttackedEffect).
/// When the buffed creature is attacked, the buff caster is healed by value+delta*level if within Range.</summary>
public readonly record struct SkillHealCastorOnAtkInfo(
    int    BaseValue,    // heal at skill level 1
    int    Delta,        // per-level scaling
    float  Range,        // max distance from buffed creature for heal to apply (0 = no range gate)
    string HealType      // "hp" | "mp" — Java HealCastorOnAttackedEffect@type attribute
);

/// <summary>"Magic counter attack" descriptor parsed from &lt;magiccounteratk&gt; (Java MagicCounterAtkEffect).
/// When the buffed creature casts a magical ATTACK skill, self-damage = min(MaxDmg, attacker.MaxHp * Percent / 100).</summary>
public readonly record struct SkillMagicCounterAtkInfo(
    int Percent,    // value attribute — percent of MaxHp consumed per magical attack
    int MaxDmg      // maxdmg attribute — cap on self-damage per cast
);

/// <summary>"Damage reflector" descriptor parsed from &lt;reflector&gt; (Java ReflectorEffect).
/// When the buffed creature is hit, attacker takes HitValue + HitDelta * SkillLevel damage back if within Radius.</summary>
public readonly record struct SkillReflectorInfo(
    int   HitValue,   // base reflect damage at level 1
    int   HitDelta,   // per-level scaling
    float Radius      // max distance from buffed creature for reflect to fire (0 = no range gate)
);

/// <summary>"Convert damage to heal" descriptor parsed from &lt;convertheal&gt; (Java ConvertHealEffect).
/// When the buffed creature is hit, restore Value+Delta*SkillLevel of HealType (HP|MP).</summary>
public readonly record struct SkillConvertHealInfo(
    int    BaseValue,    // base heal at level 1
    int    Delta,        // per-level scaling
    string HealType      // "hp" | "mp" — Java ConvertHealEffect@type attribute
);

/// <summary>"Damage shield" descriptor parsed from &lt;shield&gt; (Java ShieldEffect).
/// Absorbs HitValue + HitDelta * SkillLevel damage per incoming hit.</summary>
public readonly record struct SkillShieldInfo(
    int HitValue,    // damage absorbed per hit at level 1
    int HitDelta     // per-level scaling
);

/// <summary>Captures CC and DoT effect elements from the &lt;effects&gt; block of a skill_template.</summary>
public sealed class SkillEffects
{
    [XmlAnyElement]
    public XmlElement[]? Elements { get; set; }

    public AbnormalCcFlags CcFlags =>
        Elements?.Aggregate(AbnormalCcFlags.None, (acc, e) => acc | ElementToCcFlag(e.LocalName))
        ?? AbnormalCcFlags.None;

    public bool HasDispelDebuff => Elements?.Any(e => e.LocalName is "dispeldebuff" or "dispeldebuffphysical" or "dispeldebuffmental" or "dispelnpcdebuff") == true;
    public bool HasDispelBuff   => Elements?.Any(e => e.LocalName is "dispelbuff" or "dispelnpcbuff" or "dispelbuffcounteratk") == true;
    public bool HasHostileUp    => Elements?.Any(e => e.LocalName == "hostileup")    == true;

    private static readonly HashSet<string> SnareNames        = ["snare", "absolutesnare"];

    /// <summary>
    /// Movement speed percent change from the first statup effect with a PERCENT SPEED change.
    /// Positive = faster (e.g. +30 = 30% faster). 0 means no speed buff.
    /// </summary>
    public int SpeedStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
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

    /// <summary>
    /// Attack speed percent increase from the first slow effect with a PERCENT ATTACK_SPEED change.
    /// Positive value (e.g. 30) means attacks become 30% slower. 0 means no change.
    /// </summary>
    public int SlowAttackSpeedPct
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "slow") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "ATTACK_SPEED", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int pct)) return pct;
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// Sum of all ADD PHYSICAL_DEFENSE changes from statdown effects.
    /// Negative value = pdef reduction (e.g. Weakening Severe Blow: -100 to -400).
    /// </summary>
    public int PdefAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_DEFENSE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",              StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_DEFENSE changes from statup effects (positive = increased pdef).</summary>
    public int PdefStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_DEFENSE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",              StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_DEFEND changes from statdown effects (negative = reduced magic defense).</summary>
    public int MagicDefAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_DEFEND", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_DEFEND changes from statup effects (positive = increased magic defense).</summary>
    public int MagicDefStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_DEFEND", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_ATTACK changes from statdown effects (negative = reduced attack).</summary>
    public int PhysAtkAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",             StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD EVASION changes from statdown effects (negative = reduced evasion).</summary>
    public int EvasionAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "EVASION", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",     StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_RESIST changes from statdown effects (negative = reduced MResist).</summary>
    public int MResistAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD EVASION changes from statup effects (positive = increased evasion).</summary>
    public int EvasionStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "EVASION", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",     StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_RESIST changes from statup effects (positive = increased magic resist).</summary>
    public int MResistStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAXHP changes from statdown effects (negative = reduced max HP).</summary>
    public int MaxHpAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXHP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD ATTACK_SPEED changes from statdown effects (positive ADD = slower attacks because higher ms value).</summary>
    public int AtkSpeedAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "ATTACK_SPEED", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",          StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD ATTACK_SPEED changes from statup effects (negative ADD = faster attacks because lower ms value).</summary>
    public int AtkSpeedStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "ATTACK_SPEED", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",          StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAXMP changes from statdown effects (negative = reduced max MP).</summary>
    public int MaxMpAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXMP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD BOOST_MAGICAL_SKILL changes from statup effects (positive = increased M-boost).</summary>
    public int MagicBoostStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_MAGICAL_SKILL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                 StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_CRITICAL_RESIST changes from statdown effects (negative = reduced P-crit resist).</summary>
    public int PhysCritResistAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_CRITICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_CRITICAL_RESIST changes from statup effects (positive = increased P-crit resist).</summary>
    public int PhysCritResistStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_CRITICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_CRITICAL_RESIST changes from statdown effects (negative = reduced M-crit resist).</summary>
    public int MagicCritResistAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_CRITICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                     StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_CRITICAL_RESIST changes from statup effects (positive = increased M-crit resist).</summary>
    public int MagicCritResistStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_CRITICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                     StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_CRITICAL_DAMAGE_REDUCE changes from statdown effects (negative = lower strike fortitude).</summary>
    public int StrikeFortitudeAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_CRITICAL_DAMAGE_REDUCE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                             StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_CRITICAL_DAMAGE_REDUCE changes from statup effects (positive = higher strike fortitude).</summary>
    public int StrikeFortitudeStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_CRITICAL_DAMAGE_REDUCE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                             StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_CRITICAL_DAMAGE_REDUCE changes from statdown effects (negative = lower spell fortitude).</summary>
    public int SpellFortitudeAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_CRITICAL_DAMAGE_REDUCE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_CRITICAL_DAMAGE_REDUCE changes from statup effects (positive = higher spell fortitude).</summary>
    public int SpellFortitudeStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_CRITICAL_DAMAGE_REDUCE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD BOOST_CASTING_TIME changes from statdown effects (negative = slower casting).</summary>
    public int CastTimeAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_CASTING_TIME", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD BOOST_CASTING_TIME changes from statup effects (positive = faster casting).</summary>
    public int CastTimeStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_CASTING_TIME", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD CONCENTRATION changes from statdown effects (negative = reduced concentration).</summary>
    public int ConcentrationAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "CONCENTRATION", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD CONCENTRATION changes from statup effects (positive = increased concentration).</summary>
    public int ConcentrationStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "CONCENTRATION", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGIC_SKILL_BOOST_RESIST changes from statdown effects (negative = reduced M-suppression).</summary>
    public int MagicSuppressionAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGIC_SKILL_BOOST_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGIC_SKILL_BOOST_RESIST changes from statup effects (positive = increased M-suppression).</summary>
    public int MagicSuppressionStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGIC_SKILL_BOOST_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_CRITICAL changes from statdown effects (negative = reduced P-crit rating).</summary>
    public int PhysCritAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_CRITICAL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",               StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_CRITICAL changes from statup effects (positive = increased P-crit rating).</summary>
    public int PhysCritStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_CRITICAL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",               StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_CRITICAL changes from statdown effects (negative = reduced M-crit rating).</summary>
    public int MagicCritAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_CRITICAL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",              StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_CRITICAL changes from statup effects (positive = increased M-crit rating).</summary>
    public int MagicCritStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_CRITICAL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",              StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PARRY changes from statdown effects (negative = reduced parry).</summary>
    public int ParryAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PARRY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PARRY changes from statup effects (positive = increased parry).</summary>
    public int ParryStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PARRY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD BLOCK changes from statdown effects (negative = reduced block).</summary>
    public int BlockAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BLOCK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD BLOCK changes from statup effects (positive = increased block).</summary>
    public int BlockStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BLOCK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_ACCURACY changes from statdown effects (negative = reduced M-accuracy).</summary>
    public int MagicAccAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_ACCURACY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",              StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_ACCURACY changes from statup effects (positive = increased M-accuracy).</summary>
    public int MagicAccStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_ACCURACY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",              StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_ACCURACY changes from statdown effects (negative = reduced P-accuracy).</summary>
    public int PhysAccAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_ACCURACY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",               StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_ACCURACY changes from statup effects (positive = increased P-accuracy).</summary>
    public int PhysAccStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_ACCURACY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",               StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD HEAL_BOOST changes from statup effects (positive = increased heal power).</summary>
    public int HealBoostStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "HEAL_BOOST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",        StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD BOOST_MAGICAL_SKILL changes from statdown effects (negative = reduced M-boost).</summary>
    public int MagicBoostAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_MAGICAL_SKILL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",                 StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAXMP changes from statup effects (positive = increased max MP).</summary>
    public int MaxMpStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXMP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_ATTACK changes from statdown effects (negative = reduced M-attack).</summary>
    public int MagicAtkAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statdown") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD PHYSICAL_ATTACK changes from statup effects (positive = increased P-attack).</summary>
    public int PhysAtkStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "PHYSICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",             StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAGICAL_ATTACK changes from statup effects (positive = increased M-attack).</summary>
    public int MagicAtkStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAGICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",            StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>Sum of all ADD MAXHP changes from statup effects (positive = increased max HP).</summary>
    public int MaxHpStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXHP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    private static readonly HashSet<string> HealInstantNames  = ["healinstant", "mphealinstant", "prochealinstant", "procmphealinstant", "fphealinstant", "procfphealinstant", "dphealinstant", "procdphealinstant"];
    private static readonly HashSet<string> HotNames          = ["heal", "mpheal", "fpheal", "dpheal"];
    private static readonly HashSet<string> DotNames          = ["bleed", "poison", "disease", "spellatk", "spellatkdrain"];
    private static readonly HashSet<string> DamageEffectNames = ["skillatk", "spellatkinstant", "skillatkdraininstant", "spellatkdraininstant", "procatk_instant", "dispelbuffcounteratk"];
    private static readonly HashSet<string> MpAttackEffectNames = ["mpattackinstant"];
    private static readonly HashSet<string> NoReduceEffectNames = ["noreducespellatk"];
    private static readonly HashSet<string> HealCastorOnAtkEffectNames = ["healcastoronatk"];
    private static readonly HashSet<string> MagicCounterAtkEffectNames = ["magiccounteratk"];
    private static readonly HashSet<string> ReflectorEffectNames = ["reflector"];
    private static readonly HashSet<string> ConvertHealEffectNames = ["convertheal"];
    private static readonly HashSet<string> ShieldEffectNames = ["shield"];
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
                string ht = e.LocalName switch
                {
                    "healinstant"        => "hp",
                    "prochealinstant"    => "hp",
                    "fphealinstant"      => "fp",
                    "procfphealinstant"  => "fp",
                    "dphealinstant"      => "dp",
                    "procdphealinstant"  => "dp",
                    _                    => "mp", // mphealinstant, procmphealinstant
                };
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
                string ht = e.LocalName switch
                {
                    "heal"   => "hp",
                    "fpheal" => "fp",
                    "dpheal" => "dp",
                    _        => "mp",
                };
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
                int.TryParse(e.GetAttribute("hp_percent"), out int hpPct);
                int.TryParse(e.GetAttribute("mp_percent"), out int mpPct);
                list.Add(new(check, val, dlt, dur, e.LocalName, e.GetAttribute("element") ?? string.Empty, hpPct, mpPct));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillDamageInfo> DamageEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillDamageInfo>();
            foreach (var e in Elements)
            {
                if (!DamageEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                string dmgType = e.LocalName is "skillatk" or "skillatkdraininstant" ? "physical" : "magical";
                string element = e.GetAttribute("element") ?? string.Empty;
                int.TryParse(e.GetAttribute("accmod2"), out int accMod);
                int.TryParse(e.GetAttribute("hp_percent"), out int hpPct);
                int.TryParse(e.GetAttribute("mp_percent"), out int mpPct);
                bool noResist = string.Equals(e.GetAttribute("noresist"), "true", StringComparison.OrdinalIgnoreCase);
                list.Add(new(val, dlt, dmgType, element, accMod, hpPct, mpPct, e.LocalName, noResist));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillMpAttackInfo> MpAttackEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillMpAttackInfo>();
            foreach (var e in Elements)
            {
                if (!MpAttackEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                string element = e.GetAttribute("element") ?? string.Empty;
                list.Add(new(val, dlt, pct, element));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillHealCastorOnAtkInfo> HealCastorOnAtkEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillHealCastorOnAtkInfo>();
            foreach (var e in Elements)
            {
                if (!HealCastorOnAtkEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                float range = 0f;
                float.TryParse(e.GetAttribute("range"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out range);
                string ht = (e.GetAttribute("type") ?? "HP").ToLowerInvariant() == "mp" ? "mp" : "hp";
                list.Add(new(val, dlt, range, ht));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillMagicCounterAtkInfo> MagicCounterAtkEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillMagicCounterAtkInfo>();
            foreach (var e in Elements)
            {
                if (!MagicCounterAtkEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int pct);
                int.TryParse(e.GetAttribute("maxdmg"), out int max);
                list.Add(new(pct, max));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillReflectorInfo> ReflectorEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillReflectorInfo>();
            foreach (var e in Elements)
            {
                if (!ReflectorEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("hitvalue"), out int hit);
                int.TryParse(e.GetAttribute("hitdelta"), out int hitDlt);
                float radius = 0f;
                float.TryParse(e.GetAttribute("radius"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out radius);
                list.Add(new(hit, hitDlt, radius));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillConvertHealInfo> ConvertHealEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillConvertHealInfo>();
            foreach (var e in Elements)
            {
                if (!ConvertHealEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                string ht = (e.GetAttribute("type") ?? "HP").ToLowerInvariant() == "mp" ? "mp" : "hp";
                list.Add(new(val, dlt, ht));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillShieldInfo> ShieldEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillShieldInfo>();
            foreach (var e in Elements)
            {
                if (!ShieldEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("hitvalue"), out int hit);
                int.TryParse(e.GetAttribute("hitdelta"), out int hitDlt);
                list.Add(new(hit, hitDlt));
            }
            return list;
        }
    }

    public IReadOnlyList<SkillNoReduceInfo> NoReduceEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillNoReduceInfo>();
            foreach (var e in Elements)
            {
                if (!NoReduceEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                string element = e.GetAttribute("element") ?? string.Empty;
                list.Add(new(val, dlt, pct, element));
            }
            return list;
        }
    }

    /// <summary>True when any effect element is a &lt;resurrect&gt; (Java ResurrectEffect).</summary>
    public bool HasResurrectEffect
    {
        get
        {
            if (Elements is null) return false;
            foreach (var e in Elements)
                if (e.LocalName == "resurrect") return true;
            return false;
        }
    }

    /// <summary>The skill_id attribute of the first &lt;resurrect&gt; effect element, or 0 if absent.</summary>
    public int ResurrectSkillId
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "resurrect")
                {
                    int.TryParse(e.GetAttribute("skill_id"), out int sid);
                    return sid;
                }
            return 0;
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
