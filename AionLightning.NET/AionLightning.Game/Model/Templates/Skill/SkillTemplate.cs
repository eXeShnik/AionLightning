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

    [XmlElement("properties")]      public SkillProperties?      Properties      { get; set; }
    [XmlElement("startconditions")] public SkillStartConditions? StartConditions { get; set; }
    [XmlElement("effects")]         public SkillEffects?         Effects         { get; set; }
    [XmlElement("actions")]         public SkillActions?         Actions         { get; set; }

    /// <summary>M270: chain category required for this skill to be a valid next-link cast (empty = no chain restriction).</summary>
    public string ChainCategory => StartConditions?.Chain?.Category ?? string.Empty;

    /// <summary>M269: parsed allowed-weapon-types HashSet from &lt;startconditions&gt;&lt;weapon weapon="X Y Z"/&gt;. Empty = no restriction.</summary>
    public HashSet<string> AllowedWeapons
    {
        get
        {
            var raw = StartConditions?.Weapon?.WeaponList;
            if (string.IsNullOrWhiteSpace(raw)) return new();
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var w in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                set.Add(w);
            return set;
        }
    }

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

    /// <summary>M316: DP cost consumed on cast; 0 = no cost. Source: &lt;actions&gt;&lt;dpuse value="X"/&gt;.</summary>
    public int DpUseCost => Actions?.DpUse?.Value ?? 0;

    /// <summary>M316: HP cost consumed on cast; Value=0 means no cost. Source: &lt;actions&gt;&lt;hpuse value="V" delta="D" ratio="true"/&gt;.
    /// Flat cost = Value + Delta*(level-1); if IsRatio, treat as percent of MaxHp.</summary>
    public (int Value, int Delta, bool IsRatio) HpUseCost
    {
        get
        {
            var h = Actions?.HpUse;
            if (h is null) return (0, 0, false);
            return (h.Value, h.Delta, h.IsRatio);
        }
    }
}

/// <summary>M269+M270: skill startconditions block — &lt;weapon&gt;, &lt;chain&gt;.</summary>
public sealed class SkillStartConditions
{
    [XmlElement("weapon")] public SkillWeaponCondition? Weapon { get; set; }
    [XmlElement("chain")]  public SkillChainCondition?  Chain  { get; set; }
}

public sealed class SkillWeaponCondition
{
    /// <summary>Space-separated list of allowed weapon types (e.g. "SWORD_2H BOW MACE_1H").</summary>
    [XmlAttribute("weapon")] public string WeaponList { get; set; } = string.Empty;
}

/// <summary>M270: chain-skill startcondition — skill can only be cast as the next link of category X.</summary>
public sealed class SkillChainCondition
{
    [XmlAttribute("category")] public string Category { get; set; } = string.Empty;
}

/// <summary>M316: &lt;actions&gt; block — DP and HP cast costs.</summary>
public sealed class SkillActions
{
    [XmlElement("dpuse")] public SkillDpUse? DpUse { get; set; }
    [XmlElement("hpuse")] public SkillHpUse? HpUse { get; set; }
}

public sealed class SkillDpUse
{
    [XmlAttribute("value")] public int Value { get; set; }
}

public sealed class SkillHpUse
{
    [XmlAttribute("value")] public int Value   { get; set; }
    [XmlAttribute("delta")] public int Delta   { get; set; }
    [XmlAttribute("ratio")] public bool IsRatio { get; set; }
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

/// <summary>M287: "heal caster when target dies" descriptor parsed from &lt;healcastorontargetdead&gt; (Java HealCastorOnTargetDeadEffect).
/// The buff is applied to a target; when that target dies, the buff caster (effector) is healed if within Range.</summary>
public readonly record struct SkillHealOnTargetDeadInfo(
    int    BaseValue,
    int    Delta,
    float  Range,
    string HealType,    // "hp" | "mp"
    bool   HealParty    // healparty="true" — also heal the caster's party in range
);

/// <summary>"Magic counter attack" descriptor parsed from &lt;magiccounteratk&gt; (Java MagicCounterAtkEffect).
/// When the buffed creature casts a magical ATTACK skill, self-damage = min(MaxDmg, attacker.MaxHp * Percent / 100).</summary>
public readonly record struct SkillMagicCounterAtkInfo(
    int Percent,    // value attribute — percent of MaxHp consumed per magical attack
    int MaxDmg      // maxdmg attribute — cap on self-damage per cast
);

/// <summary>M297: delayed magical damage descriptor parsed from &lt;delaydamage&gt; (Java DelayedSpellAttackInstantEffect).</summary>
public readonly record struct SkillDelayDamageInfo(
    int    DelayMs,    // delay= attribute in milliseconds
    int    BaseValue,  // value= — base damage at level 1
    int    Delta,      // delta= — per-level scaling
    string Element     // element= — e.g. "FIRE", "WIND", etc.
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

/// <summary>"Damage protect" descriptor parsed from &lt;protect&gt; (Java ProtectEffect, shieldType=8).
/// Redirects HitValue (or HitValue% if IsPercent) of incoming damage to the buff caster.</summary>
public readonly record struct SkillProtectInfo(
    int   HitValue,    // amount or percent redirected per hit
    bool  IsPercent,   // true = HitValue is percent of incoming damage
    float Radius       // max distance from buffed creature for redirect to fire
);

/// <summary>M278: parsed &lt;change stat="X" func="Y" value="N" delta="M"/&gt; child of an effect element.</summary>
public readonly record struct SkillStatChange(
    string ParentEffect,  // local name of the parent effect (statup, statboost, boostheal, etc.)
    string Stat,          // stat name e.g. "PHYSICAL_ATTACK", "HEAL_SKILL_BOOST", "MAGICAL_RESIST"
    string Func,          // "ADD" | "PERCENT" | "REPLACE"
    int    Value,         // base value at level 1
    int    Delta          // per-level scaling (often 0 for buffs since skills have separate level-X templates)
);

/// <summary>M286a: aura buff descriptor — caster-anchored periodic AoE that ticks ChildSkillId on group members within Distance.</summary>
public readonly record struct SkillAuraInfo(
    int   ChildSkillId,  // skill_id of the per-tick child skill to apply
    float Distance,      // 2D radius around caster
    float DistanceZ      // vertical altitude limit (defaults to Distance/2 when XML missing)
);

/// <summary>M288: armor mastery descriptor — pdef% bonus conditional on equipped armor type.</summary>
public readonly record struct SkillArmorMasteryInfo(
    string ArmorType,  // "CLOTHES" | "LEATHER" | "CHAIN" | "PLATE"
    int    PdefPct     // PERCENT bonus to PHYSICAL_DEFENSE when matching armor is equipped
);

/// <summary>M291: weapon mastery descriptor — PHYSICAL_ATTACK or MAGICAL_ATTACK % bonus when matching weapon is equipped.</summary>
public readonly record struct SkillWpnMasteryInfo(
    string WeaponType, // "SWORD_1H" | "DAGGER_1H" | "MACE_1H" | "SWORD_2H" | "POLEARM_2H" | "BOW" etc.
    string Stat,       // "PHYSICAL_ATTACK" or "MAGICAL_ATTACK"
    int    Pct         // PERCENT bonus value
);

/// <summary>M289: carvesignet descriptor — attributes from &lt;carvesignet&gt; XML element.</summary>
public readonly record struct CarveSignetInfo(
    int    BaseValue,       // damage value attribute
    int    Delta,           // per-level scaling
    int    SignetId,        // signetid= — base SkillId of signet buff series (e.g. 8303)
    int    SignetLvlCap,    // signetlvl= — max signet level allowed
    int    SignetLvlStart,  // signetlvlstart= — minimum starting level (0 = default 1)
    int    Prob,            // prob= — placement probability 0-100 (default 100)
    string Signet           // signet= — stack name, e.g. "SYSTEM_SKILL_SIGNET1"
);

/// <summary>M289: signetburst descriptor — attributes from &lt;signetburst&gt; XML element.</summary>
public readonly record struct SignetBurstInfo(
    int    SignetLvlMax,    // signetlvl= — max signet level the burst scales to
    string Signet,          // signet= — stack name, e.g. "SYSTEM_SKILL_SIGNET1"
    int    AccMod2          // accmod2= — magic accuracy base for per-level bonus
);

/// <summary>M280: damage modifier parsed from &lt;skillatk&gt;/&lt;spellatkinstant&gt; &lt;modifiers&gt; block.</summary>
public readonly record struct SkillDamageModifier(
    string Kind,    // "targetrace" | "targetclass" | "abnormaldamage" | other
    string Match,   // race name (PC_LIGHT_CASTLE_DOOR), class name, abnormal state name (STUMBLE/STUN)
    int    Value,  // bonus damage at level 1
    int    Delta   // per-level scaling
);

/// <summary>Nested &lt;subeffect skill_id="X" chance="Y"/&gt; element inside a damage effect — triggers a secondary skill CC on hit.</summary>
public readonly record struct SkillSubEffectInfo(
    int SkillId,  // skill template ID to apply on hit
    int Chance    // 0-100; default 100 when absent in XML
);

/// <summary>M303: top-level &lt;skilllauncher skill_id="X"/&gt; — fires the referenced sub-skill's effects on the same target (Java SkillLauncherEffect).</summary>
public readonly record struct SkillLauncherInfo(
    int SkillId  // sub-skill template ID whose effects are applied to the target
);

/// <summary>M310: cooldown reduction descriptor parsed from &lt;skillcooltimereset first_cd="A" last_cd="B" delta="D" value="V"/&gt; (Java SkillCooltimeResetEffect).
/// When Delta &gt; 0: remaining -= remaining * Delta / 100 (e.g. delta=100 = full reset, delta=30 = shorten by 30%).
/// When Delta == 0: remaining -= Value ms (flat reduction).</summary>
public readonly record struct SkillCooldownResetInfo(
    int FirstCd,  // first cooldown ID in the range to reset
    int LastCd,   // last cooldown ID in the range to reset
    int Delta,    // percentage reduction (0-100); if >0 takes precedence over Value
    int Value     // flat ms reduction; used only when Delta == 0
);

/// <summary>M306: periodic MP drain descriptor parsed from &lt;mpattack checktime="N" value="V" delta="D" duration2="T" percent="true/false"/&gt; (Java MpAttackEffect).</summary>
public readonly record struct SkillMpAttackDotInfo(
    int  CheckTimeMs,   // tick interval in ms
    int  BaseValue,     // MP drain per tick at level 1 (flat or % of MaxMp when IsPercent)
    int  Delta,         // per-level scaling
    int  Duration2Ms,   // total effect duration in ms
    bool IsPercent      // true = drain BaseValue% of target MaxMp per tick
);

/// <summary>Captures CC and DoT effect elements from the &lt;effects&gt; block of a skill_template.</summary>
public sealed class SkillEffects
{
    [XmlAnyElement]
    public XmlElement[]? Elements { get; set; }

    public AbnormalCcFlags CcFlags =>
        Elements?.Aggregate(AbnormalCcFlags.None, (acc, e) => acc | ElementToCcFlag(e.LocalName))
        ?? AbnormalCcFlags.None;

    public bool HasDispelDebuff => Elements?.Any(e => e.LocalName is "dispeldebuff" or "dispeldebuffphysical" or "dispeldebuffmental" or "dispelnpcdebuff" or "dispel") == true;
    public bool HasDispelBuff   => Elements?.Any(e => e.LocalName is "dispelbuff" or "dispelnpcbuff" or "dispelbuffcounteratk") == true;
    public bool HasHostileUp    => Elements?.Any(e => e.LocalName == "hostileup")    == true;
    public bool HasSanctuary    => Elements?.Any(e => e.LocalName == "sanctuary")    == true;

    /// <summary>M275: model-swap effect present (shapechange/polymorph/deform/form). Behavior needs SM_TRANSFORM packet.</summary>
    public bool HasShapeChange  => Elements?.Any(e => e.LocalName is "shapechange" or "polymorph" or "deform" or "form") == true;

    /// <summary>M275: model id from first shapechange-family element (0 = no transform parsed).</summary>
    public int ShapeChangeModelId
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName is "shapechange" or "polymorph" or "deform" or "form")
                {
                    int.TryParse(e.GetAttribute("model"), out int m);
                    return m;
                }
            return 0;
        }
    }

    /// <summary>M302: duration in ms of the shape-change effect (from the first shapechange/polymorph/deform/form element's duration2 attribute).</summary>
    public int ShapeChangeDurationMs
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("shapechange" or "polymorph" or "deform" or "form")) continue;
                if (int.TryParse(e.GetAttribute("duration2"), out int d) && d > 0) return d;
            }
            return 0;
        }
    }

    /// <summary>M275: stealth/hide effect present. Behavior needs per-creature visibility filter on packet broadcasts.</summary>
    public bool HasHide         => Elements?.Any(e => e.LocalName == "hide")         == true;

    /// <summary>M301: duration of the hide/stealth effect in ms (from the &lt;hide duration2="N"/&gt; element).</summary>
    public int HideDurationMs
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "hide") continue;
                if (int.TryParse(e.GetAttribute("duration2"), out int d) && d > 0) return d;
            }
            return 0;
        }
    }

    /// <summary>M275: AbsoluteStatBuff present. Behavior needs external AbsoluteStatsData.xml loader keyed by statsetid.</summary>
    public bool HasAbsStatBuff  => Elements?.Any(e => e.LocalName is "absstatbuff" or "absstatdebuff") == true;

    /// <summary>M276: alwaysresist — full magic-damage immunity while buff active (Java AlwaysResistEffect).</summary>
    public bool HasAlwaysResist => Elements?.Any(e => e.LocalName == "alwaysresist") == true;
    /// <summary>M313: alwaysblock — guaranteed physical block for up to value hits or duration2 ms (Java AlwaysBlockEffect).</summary>
    public bool HasAlwaysBlock  => Elements?.Any(e => e.LocalName == "alwaysblock")  == true;
    /// <summary>M313: alwaysdodge — guaranteed physical dodge for up to value hits or duration2 ms (Java AlwaysDodgeEffect).</summary>
    public bool HasAlwaysDodge  => Elements?.Any(e => e.LocalName == "alwaysdodge")  == true;

    public int AlwaysBlockCount
    {
        get { var el = Elements?.FirstOrDefault(e => e.LocalName == "alwaysblock"); return el is not null && int.TryParse(el.GetAttribute("value"), out int v) ? v : 0; }
    }
    public int AlwaysBlockDurationMs
    {
        get { var el = Elements?.FirstOrDefault(e => e.LocalName == "alwaysblock"); return el is not null && int.TryParse(el.GetAttribute("duration2"), out int v) ? v : 0; }
    }
    public int AlwaysDodgeCount
    {
        get { var el = Elements?.FirstOrDefault(e => e.LocalName == "alwaysdodge"); return el is not null && int.TryParse(el.GetAttribute("value"), out int v) ? v : 0; }
    }
    public int AlwaysDodgeDurationMs
    {
        get { var el = Elements?.FirstOrDefault(e => e.LocalName == "alwaysdodge"); return el is not null && int.TryParse(el.GetAttribute("duration2"), out int v) ? v : 0; }
    }

    /// <summary>M331: alwaysparry — guaranteed physical parry for up to value hits or duration2 ms (Java AlwaysParryEffect).</summary>
    public bool HasAlwaysParry  => Elements?.Any(e => e.LocalName == "alwaysparry")  == true;
    public int AlwaysParryCount
    {
        get { var el = Elements?.FirstOrDefault(e => e.LocalName == "alwaysparry"); return el is not null && int.TryParse(el.GetAttribute("value"), out int v) ? v : 0; }
    }
    public int AlwaysParryDurationMs
    {
        get { var el = Elements?.FirstOrDefault(e => e.LocalName == "alwaysparry"); return el is not null && int.TryParse(el.GetAttribute("duration2"), out int v) ? v : 0; }
    }

    /// <summary>M331: noresurrectpenalty — while active, death does not apply soul sickness (Java NoresurrectpenaltyEffect).</summary>
    public bool HasNoresurrectPenalty => Elements?.Any(e => e.LocalName == "noresurrectpenalty") == true;

    /// <summary>M282: flight-ban debuff — buffed creature loses fly capability (Java NoFlyEffect). Behavior needs flight-state subsystem.</summary>
    public bool HasNoFly        => Elements?.Any(e => e.LocalName == "nofly")        == true;
    /// <summary>M285: provoker buff — buffed NPC auto-targets last attacker (Java ProvokerEffect ATTACK observer).</summary>
    public bool HasProvoker     => Elements?.Any(e => e.LocalName == "provoker")     == true;

    /// <summary>M304: closeaerial — removes the OpenAerial (aerial launch) effect from the target on hit (Java CloseAerialEffect removes skill 8224).</summary>
    public bool HasCloseAerial  => Elements?.Any(e => e.LocalName == "closeaerial")  == true;

    /// <summary>M286a: aura buff present (Java AuraEffect) — caster-anchored periodic AoE on group/self in range.</summary>
    public bool HasAura         => Elements?.Any(e => e.LocalName == "aura")         == true;

    /// <summary>M286a: parsed &lt;aura skill_id="X" distance="Y" distance_z="Z"/&gt;. distance_z defaults to distance/2 when missing.</summary>
    public IReadOnlyList<SkillAuraInfo> AuraEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillAuraInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "aura") continue;
                int.TryParse(e.GetAttribute("skill_id"), out int childId);
                float distance = 0f;
                float.TryParse(e.GetAttribute("distance"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out distance);
                float distanceZ;
                if (!float.TryParse(e.GetAttribute("distance_z"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out distanceZ))
                    distanceZ = distance / 2f;
                if (childId > 0 && distance > 0f)
                    list.Add(new(childId, distance, distanceZ));
            }
            return list;
        }
    }

    /// <summary>M288: parsed &lt;armormastery armor="X"&gt; effects — conditional pdef% bonus.</summary>
    public IReadOnlyList<SkillArmorMasteryInfo> ArmorMasteryEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillArmorMasteryInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "armormastery") continue;
                string armorType = e.GetAttribute("armor") ?? string.Empty;
                if (armorType.Length == 0) continue;
                int pct = 0;
                foreach (System.Xml.XmlNode child in e.ChildNodes)
                {
                    if (child is System.Xml.XmlElement ce
                        && ce.LocalName == "change"
                        && string.Equals(ce.GetAttribute("stat"), "PHYSICAL_DEFENSE", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(ce.GetAttribute("value"), out int v)
                        && v > 0)
                    {
                        pct += v;
                    }
                }
                if (pct > 0)
                    list.Add(new(armorType, pct));
            }
            return list;
        }
    }

    /// <summary>M291: parsed &lt;wpnmastery weapon="X"&gt;&lt;change stat="Y" func="PERCENT" value="Z"/&gt; — weapon proficiency passive bonuses.</summary>
    public IReadOnlyList<SkillWpnMasteryInfo> WpnMasteryEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillWpnMasteryInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "wpnmastery") continue;
                string weaponType = e.GetAttribute("weapon") ?? string.Empty;
                if (weaponType.Length == 0) continue;
                foreach (System.Xml.XmlNode child in e.ChildNodes)
                {
                    if (child is System.Xml.XmlElement ce
                        && ce.LocalName == "change"
                        && string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(ce.GetAttribute("value"), out int v)
                        && v > 0)
                    {
                        string stat = ce.GetAttribute("stat") ?? string.Empty;
                        if (stat.Length > 0)
                            list.Add(new(weaponType, stat, v));
                    }
                }
            }
            return list;
        }
    }

    /// <summary>M289: carvesignet descriptor — place/upgrade SYSTEM_SKILL_SIGNET1 buff on target after damage.</summary>
    public IReadOnlyList<CarveSignetInfo> CarveSignetEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<CarveSignetInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "carvesignet") continue;
                int.TryParse(e.GetAttribute("value"),          out int val);
                int.TryParse(e.GetAttribute("delta"),          out int dlt);
                int.TryParse(e.GetAttribute("signetid"),       out int sid);
                int.TryParse(e.GetAttribute("signetlvl"),      out int cap);
                int.TryParse(e.GetAttribute("signetlvlstart"), out int start);
                int prob = 100;
                if (int.TryParse(e.GetAttribute("prob"), out int p)) prob = p;
                string signet = e.GetAttribute("signet") ?? "NONE";
                list.Add(new(val, dlt, sid, cap, start, prob, signet));
            }
            return list;
        }
    }

    /// <summary>M289: signetburst descriptor — scale damage by signet level and consume the signet.</summary>
    public IReadOnlyList<SignetBurstInfo> SignetBurstEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SignetBurstInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "signetburst") continue;
                int.TryParse(e.GetAttribute("signetlvl"), out int cap);
                int.TryParse(e.GetAttribute("accmod2"),   out int acc);
                string signet = e.GetAttribute("signet") ?? "NONE";
                list.Add(new(cap, signet, acc));
            }
            return list;
        }
    }

    /// <summary>M283: rebirth (self-rez on death) — Java RebirthEffect. (Has, ResurrectPercent, SkillId)</summary>
    public (bool Has, int ResurrectPercent, int SkillId) RebirthInfo
    {
        get
        {
            if (Elements is null) return (false, 0, 0);
            foreach (var e in Elements)
            {
                if (e.LocalName != "rebirth") continue;
                int.TryParse(e.GetAttribute("resurrect_percent"), out int pct);
                int.TryParse(e.GetAttribute("skill_id"), out int sid);
                return (true, pct > 0 ? pct : 5, sid);
            }
            return (false, 0, 0);
        }
    }

    /// <summary>M281: FP-damage value from &lt;fpatkinstant&gt;/&lt;delayedfpatk_instant&gt;. (Value, Delta, IsPercent) — IsPercent: drains pct of MaxFp.</summary>
    public (int Value, int Delta, bool IsPercent) FpAttackInfo
    {
        get
        {
            if (Elements is null) return (0, 0, false);
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("fpatkinstant" or "delayedfpatk_instant")) continue;
                int.TryParse(e.GetAttribute("value"), out int v);
                int.TryParse(e.GetAttribute("delta"), out int d);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                return (v, d, pct);
            }
            return (0, 0, false);
        }
    }

    /// <summary>M280: damage modifiers parsed from any damage-effect element's &lt;modifiers&gt; block. Bonus damage gated by target attribute.</summary>
    public IReadOnlyList<SkillDamageModifier> DamageModifiers
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillDamageModifier>();
            foreach (var e in Elements)
            {
                foreach (System.Xml.XmlNode child in e.ChildNodes)
                {
                    if (child is not System.Xml.XmlElement modWrap) continue;
                    if (modWrap.LocalName != "modifiers") continue;
                    foreach (System.Xml.XmlNode mNode in modWrap.ChildNodes)
                    {
                        if (mNode is not System.Xml.XmlElement m) continue;
                        string kind = m.LocalName;
                        string match = m.GetAttribute("race") is { Length: > 0 } r ? r
                                     : m.GetAttribute("class") is { Length: > 0 } cl ? cl
                                     : m.GetAttribute("state") is { Length: > 0 } st ? st
                                     : string.Empty;
                        int.TryParse(m.GetAttribute("value"), out int val);
                        int.TryParse(m.GetAttribute("delta"), out int dlt);
                        list.Add(new(kind, match, val, dlt));
                    }
                }
            }
            return list;
        }
    }

    /// <summary>M278: all parsed &lt;change&gt; children across statup/statboost/boost*/deboost*/wpnmastery/armormastery/etc. Foundation for passive stat engine.</summary>
    public IReadOnlyList<SkillStatChange> StatChanges
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillStatChange>();
            foreach (var e in Elements)
            {
                foreach (System.Xml.XmlNode child in e.ChildNodes)
                {
                    if (child is not System.Xml.XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    string stat = ce.GetAttribute("stat") ?? string.Empty;
                    if (stat.Length == 0) continue;
                    string func = (ce.GetAttribute("func") ?? "ADD").ToUpperInvariant();
                    int.TryParse(ce.GetAttribute("value"), out int val);
                    int.TryParse(ce.GetAttribute("delta"), out int dlt);
                    list.Add(new(e.LocalName, stat, func, val, dlt));
                }
            }
            return list;
        }
    }

    /// <summary>M274: per-tick MP cost from &lt;periodicactions checktime="X"&gt;&lt;mpuse value="Y"/&gt;&lt;/periodicactions&gt;.
    /// (CheckTimeMs, MpPerTick) — both 0 when no periodic actions defined.</summary>
    public (int CheckTimeMs, int MpPerTick) PeriodicMpUse
    {
        get
        {
            if (Elements is null) return (0, 0);
            foreach (var e in Elements)
            {
                if (e.LocalName != "periodicactions") continue;
                int.TryParse(e.GetAttribute("checktime"), out int check);
                foreach (System.Xml.XmlNode child in e.ChildNodes)
                {
                    if (child is not System.Xml.XmlElement ce) continue;
                    if (ce.LocalName != "mpuse") continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v) && v > 0)
                        return (check, v);
                }
            }
            return (0, 0);
        }
    }

    /// <summary>M317: per-tick HP cost from &lt;periodicactions checktime="X"&gt;&lt;hpuse value="Y" delta="D"/&gt;&lt;/periodicactions&gt;.
    /// (CheckTimeMs, HpValue, HpDelta) — CheckTimeMs=0 when no periodic HP drain defined.</summary>
    public (int CheckTimeMs, int HpValue, int HpDelta) PeriodicHpUse
    {
        get
        {
            if (Elements is null) return (0, 0, 0);
            foreach (var e in Elements)
            {
                if (e.LocalName != "periodicactions") continue;
                int.TryParse(e.GetAttribute("checktime"), out int check);
                foreach (System.Xml.XmlNode child in e.ChildNodes)
                {
                    if (child is not System.Xml.XmlElement ce) continue;
                    if (ce.LocalName != "hpuse") continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v) && v > 0)
                    {
                        int.TryParse(ce.GetAttribute("delta"), out int d);
                        return (check, v, d);
                    }
                }
            }
            return (0, 0, 0);
        }
    }

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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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

    /// <summary>M325: sum of PERCENT MAGICAL_RESIST changes from statup/statboost elements (positive = % mresist increased).</summary>
    public int MResistStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "MAGICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",         StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M325: sum of PERCENT EVASION changes from statup/statboost elements (positive = % evasion increased).</summary>
    public int EvasionStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "EVASION", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M324: sum of PERCENT PHYSICAL_DEFENSE changes from statup/statboost elements (positive = % pdef increased).</summary>
    public int PdefStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_DEFENSE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M324: sum of PERCENT ATTACK_SPEED changes from statup/statboost elements (negative = % faster attacks; positive = % slower).</summary>
    public int AtkSpeedStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "ATTACK_SPEED", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",       StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M323: sum of PERCENT PHYSICAL_ATTACK changes from statup/statboost elements (positive = % patk increased).</summary>
    public int PhysAtkStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",          StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M323: sum of PERCENT MAGICAL_ATTACK changes from statup/statboost elements (positive = % matk increased).</summary>
    public int MagicAtkStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "MAGICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",         StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M330: sum of PERCENT REGEN_HP changes from statup/statboost elements (positive = % HP regen rate increase per tick).</summary>
    public int RegenHpStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "REGEN_HP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",  StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M331: sum of PERCENT REGEN_FP changes from statup/statboost elements (positive = % FP regen rate increase per tick).</summary>
    public int RegenFpStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "REGEN_FP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",  StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M330: sum of PERCENT REGEN_MP changes from statup/statboost elements (positive = % MP regen rate increase per tick).</summary>
    public int RegenMpStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "REGEN_MP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",  StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M329: sum of PERCENT MAGICAL_DEFEND changes from statup elements (positive = % of current magic defense added).</summary>
    public int MagicDefStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "MAGICAL_DEFEND", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",        StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M327: sum of PERCENT PHYSICAL_CRITICAL changes from statup/statboost elements (positive = % of current crit rating added).</summary>
    public int PhysCritStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_CRITICAL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M327: sum of PERCENT PHYSICAL_ACCURACY changes from statup elements (positive = % of current phys acc added).</summary>
    public int PhysAccStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_ACCURACY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M327: sum of PERCENT PHYSICAL_ACCURACY changes from statdown elements (negative = % of target phys acc reduced).</summary>
    public int PhysAccPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_ACCURACY", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M328: sum of PERCENT BOOST_MAGICAL_SKILL changes from statdown elements (negative = % of magic boost reduced).</summary>
    public int MagicBoostPctDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "BOOST_MAGICAL_SKILL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",             StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M328: sum of PERCENT BOOST_MAGICAL_SKILL changes from statup/statboost elements (positive = % of magic boost added).</summary>
    public int MagicBoostStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "BOOST_MAGICAL_SKILL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",             StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M328: sum of PERCENT BLOCK changes from statup/statboost elements (positive = % of current block rating added).</summary>
    public int BlockStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "BLOCK",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M328: sum of PERCENT BLOCK changes from statdown elements (negative = % of current block rating reduced).</summary>
    public int BlockPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "BLOCK",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M328: sum of PERCENT PARRY changes from statup elements (positive = % of current parry rating added).</summary>
    public int ParryStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"),  "PARRY",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M326: sum of PERCENT PHYSICAL_ATTACK changes from statdown elements (negative = % of target patk reduced).</summary>
    public int PatkPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",          StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M326: sum of PERCENT EVASION changes from statdown elements (negative = % of target evasion reduced).</summary>
    public int EvasionPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "EVASION", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M326: sum of PERCENT MAGICAL_ATTACK changes from statdown elements (negative = % of target matk reduced).</summary>
    public int MagicAtkPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "MAGICAL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",         StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M322: sum of PERCENT PHYSICAL_DEFENSE changes from statdown elements (negative = % of target pdef reduced).</summary>
    public int PdefPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "PHYSICAL_DEFENSE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",           StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M322: sum of PERCENT MAGICAL_RESIST changes from statdown elements (negative = % of target mresist reduced).</summary>
    public int MResistPercentDebuff
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "MAGICAL_RESIST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",         StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M320: fly speed percent change from statdown elements with PERCENT FLY_SPEED changes.
    /// Typically negative (e.g. -50 = halve fly speed). 0 means no change.</summary>
    public int StatdownFlySpeedPct
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "FLY_SPEED", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M318: movement speed percent change from statdown elements with PERCENT SPEED changes.
    /// Typically negative (e.g. -30 = 30% slower). 0 means no change.</summary>
    public int StatdownSpeedPct
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
                    if (!string.Equals(ce.GetAttribute("stat"),  "SPEED",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"),  "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
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

    /// <summary>M292: miss chance % from the first blind element's value= attribute (e.g. 80 → 80% miss).</summary>
    public int BlindDodgePct
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "blind") continue;
                if (int.TryParse(e.GetAttribute("value"), out int pct)) return pct;
            }
            return 0;
        }
    }

    /// <summary>M294: HEAL_SKILL_DEBOOST percent from deboostheal child change (negative = less healing received).</summary>
    public int HealDeboostPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "deboostheal") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "HEAL_SKILL_DEBOOST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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

    /// <summary>Sum of all ADD MAXHP changes from statdown/curse effects (negative = reduced max HP).</summary>
    public int MaxHpAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statdown" or "curse")) continue;
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

    /// <summary>M307: sum of all PERCENT MAXHP changes from statdown/curse effects (negative = % of target MaxHp reduced).</summary>
    public int MaxHpPercentDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statdown" or "curse")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXHP",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M307: sum of all PERCENT MAXMP changes from statdown/curse effects (negative = % of target MaxMp reduced).</summary>
    public int MaxMpPercentDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statdown" or "curse")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXMP",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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

    /// <summary>Sum of all ADD MAXMP changes from statdown/curse effects (negative = reduced max MP).</summary>
    public int MaxMpAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statdown" or "curse")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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

    /// <summary>M296: off-hand damage effectiveness % from the first wpndual element's value= attribute (e.g. 70 → 70%).</summary>
    public int WpnDualEffectPct
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "wpndual") continue;
                if (int.TryParse(e.GetAttribute("value"), out int pct)) return pct;
            }
            return 0;
        }
    }

    /// <summary>M293: PERCENT BOOST_CASTING_TIME from boostskillcastingtime elements, converted to permille (value*10) for the cast-time formula.</summary>
    public int BoostCastTimePctDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "boostskillcastingtime") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_CASTING_TIME", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v * 10;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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

    /// <summary>M311: sum of all PERCENT MAXHP changes from statup/statboost passive skills (positive = % of base MaxHp added).</summary>
    public int MaxHpPercentStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXHP",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M311: sum of all PERCENT MAXMP changes from statup/statboost passive skills (positive = % of base MaxMp added).</summary>
    public int MaxMpPercentStatUpDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "MAXMP",   StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M315: sum of PERCENT HEAL_SKILL_BOOST from non-onfly boostheal child changes (passive heal-mastery skills).</summary>
    public int BoostHealSkillBoostPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "boostheal") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "HEAL_SKILL_BOOST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT",          StringComparison.OrdinalIgnoreCase)) continue;
                    bool hasOnFly = false;
                    foreach (XmlNode cond in ce.ChildNodes)
                        if (cond is XmlElement condEl && condEl.LocalName == "conditions")
                        {
                            foreach (XmlNode oc in condEl.ChildNodes)
                                if (oc is XmlElement ocEl && ocEl.LocalName == "onfly") { hasOnFly = true; break; }
                            break;
                        }
                    if (hasOnFly) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M315: sum of PERCENT BOOST_SPELL_ATTACK from non-onfly boostspellattack child changes (passive spell-attack mastery skills).</summary>
    public int BoostSpellAttackPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "boostspellattack") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_SPELL_ATTACK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT",             StringComparison.OrdinalIgnoreCase)) continue;
                    bool hasOnFly = false;
                    foreach (XmlNode cond in ce.ChildNodes)
                        if (cond is XmlElement condEl && condEl.LocalName == "conditions")
                        {
                            foreach (XmlNode oc in condEl.ChildNodes)
                                if (oc is XmlElement ocEl && ocEl.LocalName == "onfly") { hasOnFly = true; break; }
                            break;
                        }
                    if (hasOnFly) continue;
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
                if (e.LocalName is not ("statup" or "statboost")) continue;
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

    private static readonly HashSet<string> HealInstantNames  = ["healinstant", "mphealinstant", "prochealinstant", "procmphealinstant", "fphealinstant", "procfphealinstant", "dphealinstant", "procdphealinstant", "vphealinstant", "procvphealinstant"];
    private static readonly HashSet<string> HotNames          = ["heal", "mpheal", "fpheal", "dpheal"];
    private static readonly HashSet<string> DotNames          = ["bleed", "poison", "disease", "spellatk", "spellatkdrain"];
    private static readonly HashSet<string> DamageEffectNames = ["skillatk", "spellatkinstant", "skillatkdraininstant", "spellatkdraininstant", "procatk_instant", "dispelbuffcounteratk", "carvesignet", "signetburst", "dash", "movebehind", "backdash", "deathblow"];
    private static readonly HashSet<string> MpAttackEffectNames = ["mpattackinstant"];
    private static readonly HashSet<string> NoReduceEffectNames = ["noreducespellatk"];
    private static readonly HashSet<string> HealCastorOnAtkEffectNames = ["healcastoronatk"];
    private static readonly HashSet<string> HealOnTargetDeadEffectNames = ["healcastorontargetdead"];
    private static readonly HashSet<string> MagicCounterAtkEffectNames = ["magiccounteratk"];
    private static readonly HashSet<string> ReflectorEffectNames = ["reflector"];
    private static readonly HashSet<string> ConvertHealEffectNames = ["convertheal"];
    private static readonly HashSet<string> ShieldEffectNames = ["shield"];
    private static readonly HashSet<string> ProtectEffectNames = ["protect"];
    // Elements that carry debuff durations via their duration2 attribute
    // M305: CC element names added so EffectDuration returns non-zero for attack skills with tslot=DEBUFF + CC (388 skills affected)
    private static readonly HashSet<string> EffectDurNames    = [
        "slow", "snare", "absolutesnare", "statdown", "statup", "statboost", "blind", "confuse", "absoluteslow", "deboostheal", "openaerial",
        "stun", "stunalways", "buffstun", "sleep", "root", "silence", "buffsilence",
        "paralyze", "fear", "stagger", "staggeralways", "stumble", "stumblealways", "spin",
        "bind", "buffbind",
        "mpattack",
        "curse"
    ];

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
                    "vphealinstant"      => "vp",
                    "procvphealinstant"  => "vp",
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
                string dmgType = e.LocalName is "skillatk" or "skillatkdraininstant" or "dash" or "movebehind" or "backdash" ? "physical" : "magical";
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

    public IReadOnlyList<SkillHealOnTargetDeadInfo> HealOnTargetDeadEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillHealOnTargetDeadInfo>();
            foreach (var e in Elements)
            {
                if (!HealOnTargetDeadEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                float range = 0f;
                float.TryParse(e.GetAttribute("range"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out range);
                string ht = (e.GetAttribute("type") ?? "HP").ToLowerInvariant() == "mp" ? "mp" : "hp";
                bool party = string.Equals(e.GetAttribute("healparty"), "true", StringComparison.OrdinalIgnoreCase);
                list.Add(new(val, dlt, range, ht, party));
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

    public IReadOnlyList<SkillProtectInfo> ProtectEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillProtectInfo>();
            foreach (var e in Elements)
            {
                if (!ProtectEffectNames.Contains(e.LocalName)) continue;
                int.TryParse(e.GetAttribute("hitvalue"), out int hit);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                float radius = 0f;
                float.TryParse(e.GetAttribute("radius"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out radius);
                list.Add(new(hit, pct, radius));
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

    /// <summary>M297: delayed magical damage descriptors from &lt;delaydamage&gt; elements.</summary>
    public IReadOnlyList<SkillDelayDamageInfo> DelayDamageEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillDelayDamageInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "delaydamage") continue;
                if (!int.TryParse(e.GetAttribute("delay"), out int delayMs) || delayMs <= 0) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                string element = e.GetAttribute("element") ?? string.Empty;
                list.Add(new(delayMs, val, dlt, element));
            }
            return list;
        }
    }

    /// <summary>M299: nested &lt;subeffect skill_id="X" chance="Y"/&gt; elements inside any damage effect; triggers secondary CC on hit.</summary>
    public IReadOnlyList<SkillSubEffectInfo> SubEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillSubEffectInfo>();
            foreach (var e in Elements)
            {
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce || ce.LocalName != "subeffect") continue;
                    if (!int.TryParse(ce.GetAttribute("skill_id"), out int skillId) || skillId <= 0) continue;
                    int.TryParse(ce.GetAttribute("chance"), out int chance);
                    if (chance <= 0) chance = 100;
                    list.Add(new(skillId, chance));
                }
            }
            return list;
        }
    }

    /// <summary>M303: top-level &lt;skilllauncher skill_id="X"/&gt; elements — each fires the sub-skill's effects on the same target.</summary>
    public IReadOnlyList<SkillLauncherInfo> LauncherEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillLauncherInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "skilllauncher") continue;
                if (!int.TryParse(e.GetAttribute("skill_id"), out int skillId) || skillId <= 0) continue;
                list.Add(new(skillId));
            }
            return list;
        }
    }

    /// <summary>M306: parsed &lt;mpattack checktime="N" value="V" delta="D" duration2="T" percent="X"/&gt; elements — periodic MP drain.</summary>
    public IReadOnlyList<SkillMpAttackDotInfo> MpAttackDotEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillMpAttackDotInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "mpattack") continue;
                if (!int.TryParse(e.GetAttribute("checktime"), out int check) || check <= 0) continue;
                if (!int.TryParse(e.GetAttribute("duration2"), out int dur)   || dur   <= 0) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                list.Add(new(check, val, dlt, dur, pct));
            }
            return list;
        }
    }

    /// <summary>M319: parsed &lt;fpatk checktime="N" value="V" delta="D" duration2="T" percent="true/false"/&gt; elements — periodic FP drain (Java FpAtkEffect).</summary>
    public IReadOnlyList<SkillMpAttackDotInfo> FpAttackDotEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillMpAttackDotInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "fpatk") continue;
                if (!int.TryParse(e.GetAttribute("checktime"), out int check) || check <= 0) continue;
                if (!int.TryParse(e.GetAttribute("duration2"), out int dur)   || dur   <= 0) continue;
                int.TryParse(e.GetAttribute("value"), out int val);
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                bool pct = string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
                list.Add(new(check, val, dlt, dur, pct));
            }
            return list;
        }
    }

    /// <summary>M310: parsed &lt;skillcooltimereset first_cd="A" last_cd="B" delta="D" value="V"/&gt; — reduces cooldowns in a CooldownId range (Java SkillCooltimeResetEffect).</summary>
    public IReadOnlyList<SkillCooldownResetInfo> CooldownResetEffects
    {
        get
        {
            if (Elements is null) return [];
            var list = new List<SkillCooldownResetInfo>();
            foreach (var e in Elements)
            {
                if (e.LocalName != "skillcooltimereset") continue;
                if (!int.TryParse(e.GetAttribute("first_cd"), out int first) || first <= 0) continue;
                if (!int.TryParse(e.GetAttribute("last_cd"),  out int last)  || last  <= 0) continue;
                int.TryParse(e.GetAttribute("delta"), out int dlt);
                int.TryParse(e.GetAttribute("value"), out int val);
                list.Add(new(first, last, dlt, val));
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

    /// <summary>M332: sum of ADD DR_BOOST changes from statup/statboost elements (positive = % drop-rate increase, e.g. 20 = +20%).</summary>
    public int DRBoostAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "DR_BOOST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M332: sum of ADD AP_BOOST changes from statup/statboost elements (positive = % AP-gain increase, e.g. 20 = +20%).</summary>
    public int APBoostAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "AP_BOOST", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD",      StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M333: sum of ADD ABNORMAL_RESISTANCE_ALL changes from statup/statboost elements (positive = CC resist points, 0–10000 scale).</summary>
    public int CcResistAllAddDelta
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName is not ("statup" or "statboost")) continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "ABNORMAL_RESISTANCE_ALL", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "ADD", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M334: count of one-time crit-boost charges from &lt;onetimeboostskillcritical&gt; element (0 if absent).</summary>
    public int OnetimeCritCount
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillcritical" && int.TryParse(e.GetAttribute("count"), out int v)) return v;
            return 0;
        }
    }

    /// <summary>M334: crit boost value — flat crit-rating ADD when percent="false", or direct crit% when percent="true".</summary>
    public int OnetimeCritValue
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillcritical" && int.TryParse(e.GetAttribute("value"), out int v)) return v;
            return 0;
        }
    }

    /// <summary>M334: true when &lt;onetimeboostskillcritical percent="true"/&gt; — value is then a direct crit-% boost, not a crit-rating ADD.</summary>
    public bool OnetimeCritIsPercent
    {
        get
        {
            if (Elements is null) return false;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillcritical")
                    return string.Equals(e.GetAttribute("percent"), "true", StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }

    /// <summary>M334: max active duration (ms) from &lt;onetimeboostskillcritical duration2="X"/&gt;.</summary>
    public int OnetimeCritDurationMs
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillcritical" && int.TryParse(e.GetAttribute("duration2"), out int v)) return v;
            return 0;
        }
    }

    /// <summary>M334: count of one-time atk-boost charges from &lt;onetimeboostskillattack&gt; element (0 if absent).</summary>
    public int OnetimeAtkCount
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillattack" && int.TryParse(e.GetAttribute("count"), out int v)) return v;
            return 0;
        }
    }

    /// <summary>M334: percent damage boost per charge from &lt;onetimeboostskillattack value="X"/&gt; (e.g. 30 = +30% damage).</summary>
    public int OnetimeAtkPct
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillattack" && int.TryParse(e.GetAttribute("value"), out int v)) return v;
            return 0;
        }
    }

    /// <summary>M334: skill type filter from &lt;onetimeboostskillattack type="PHYSICAL|MAGICAL"/&gt;.</summary>
    public string OnetimeAtkType
    {
        get
        {
            if (Elements is null) return string.Empty;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillattack") return e.GetAttribute("type") ?? string.Empty;
            return string.Empty;
        }
    }

    /// <summary>M334: max active duration (ms) from &lt;onetimeboostskillattack duration2="X"/&gt;.</summary>
    public int OnetimeAtkDurationMs
    {
        get
        {
            if (Elements is null) return 0;
            foreach (var e in Elements)
                if (e.LocalName == "onetimeboostskillattack" && int.TryParse(e.GetAttribute("duration2"), out int v)) return v;
            return 0;
        }
    }

    /// <summary>M333: sum of PERCENT BOOST_HATE changes from boosthate elements (positive = % more hate generated, negative = less).</summary>
    public int BoostHateStatPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "boosthate") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_HATE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M336: BOOST_HUNTING_XP_RATE ADD from xpboost elements (positive = % more XP from solo NPC kills, e.g. 30 = +30%).</summary>
    public int HuntingXpBoostPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "xpboost") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_HUNTING_XP_RATE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M336: BOOST_GROUP_HUNTING_XP_RATE ADD from xpboost elements (positive = % more XP from group NPC kills, e.g. 30 = +30%).</summary>
    public int GroupHuntingXpBoostPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "xpboost") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "BOOST_GROUP_HUNTING_XP_RATE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int v)) total += v;
                }
            }
            return total;
        }
    }

    /// <summary>M335: PERCENT FLY_TIME change from statup/statboost elements (positive = % more max FP, e.g. 400 = +400% base MaxFp).</summary>
    public int FlyTimeStatUpPct
    {
        get
        {
            if (Elements is null) return 0;
            int total = 0;
            foreach (var e in Elements)
            {
                if (e.LocalName != "statup" && e.LocalName != "statboost") continue;
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement ce) continue;
                    if (ce.LocalName != "change") continue;
                    if (!string.Equals(ce.GetAttribute("stat"), "FLY_TIME", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(ce.GetAttribute("func"), "PERCENT", StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(ce.GetAttribute("value"), out int pct)) total += pct;
                }
            }
            return total;
        }
    }

    private static AbnormalCcFlags ElementToCcFlag(string name) => name switch
    {
        "stun" or "stunalways" or "buffstun"          => AbnormalCcFlags.Stun,
        "sleep"                                       => AbnormalCcFlags.Sleep,
        "root"                                        => AbnormalCcFlags.Root,
        "silence" or "buffsilence"                    => AbnormalCcFlags.Silence,
        "bind" or "buffbind"                          => AbnormalCcFlags.Root,   // BIND = movement lock only; still allows attacking (Java BindEffect extends RootEffect)
        "paralyze"                                    => AbnormalCcFlags.Paralyze,
        "fear"                                        => AbnormalCcFlags.Fear,
        "stagger" or "staggeralways"                  => AbnormalCcFlags.Stagger,
        "stumble" or "stumblealways"                  => AbnormalCcFlags.Stumble,
        "spin"                                        => AbnormalCcFlags.Spin,
        "blind"                                       => AbnormalCcFlags.Blind,
        "openaerial"                                  => AbnormalCcFlags.OpenAerial,
        "curse"                                       => AbnormalCcFlags.Curse,
        _                                             => AbnormalCcFlags.None
    };
}
