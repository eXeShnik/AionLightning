using AionLightning.Game.Model.Templates.Skill;

namespace AionLightning.Game.Model;

/// <summary>Tracks a single active buff or debuff on a creature.</summary>
public sealed class AbnormalState
{
    public int             SkillId    { get; init; }
    public int             SkillLevel { get; init; }
    public int             EffectorId { get; init; }
    public DateTime        Expiry     { get; init; }
    public AbnormalCcFlags CcFlags    { get; init; } = AbnormalCcFlags.None;
    public bool            IsDebuff   { get; init; }   // true when applied as an enemy debuff
    public SkillDotInfo?   DotInfo    { get; init; }

    // Non-zero when this debuff reduces movement speed (e.g. snare: -50 = halve speed)
    public int   MovSpeedPct        { get; init; }
    // Target's MovementSpeed before this debuff was applied — used to restore on expiry
    public float PreDebuffSpeed     { get; init; }

    // Non-zero when this debuff increases attack speed (slow: +30 = 30% slower attacks)
    public int   AttackSpeedPct     { get; init; }
    // Target's CurrentAttackSpeed before this debuff was applied — used to restore on expiry
    public int   PreDebuffAtkSpeed  { get; init; }

    // Non-zero when this debuff reduces PHYSICAL_DEFENSE (e.g. Weakening Severe Blow: -100 to -400)
    public int   PdefDelta          { get; init; }
    // Non-zero when this debuff reduces MAGICAL_RESIST
    public int   MResistDelta       { get; init; }
    // Non-zero when this debuff reduces PHYSICAL_ATTACK
    public int   PatkDelta          { get; init; }
    // Non-zero when this debuff reduces EVASION
    public int   EvasionDelta       { get; init; }
    // Non-zero when this effect changes MAXHP (negative = debuff, positive = buff)
    public int   MaxHpDelta         { get; init; }
    // Non-zero when this debuff reduces MAGICAL_ATTACK
    public int   MagicAtkDelta      { get; init; }
    // Non-zero when this debuff increases ATTACK_SPEED ms via statdown ADD (positive = slower)
    public int   AtkSpeedDelta      { get; init; }
    // Non-zero when this effect changes MAXMP (negative = debuff, positive = buff)
    public int   MaxMpDelta         { get; init; }
    // Non-zero when this effect changes BOOST_MAGICAL_SKILL (positive = buff, negative = debuff)
    public int   MagicBoostDeltaVal { get; init; }
    // Non-zero when this buff increases HEAL_BOOST (positive = heals stronger)
    public int   HealBoostDeltaVal  { get; init; }
    // Non-zero when this effect changes PHYSICAL_ACCURACY (negative = debuff, positive = buff/statup)
    public int   PhysAccDeltaVal    { get; init; }
    // Non-zero when this effect changes MAGICAL_ACCURACY (negative = debuff, positive = buff/statup)
    public int   MagicAccDeltaVal   { get; init; }
    // Non-zero when this effect changes PARRY (negative = debuff, positive = buff/statup)
    public int   ParryDeltaVal      { get; init; }
    // Non-zero when this effect changes BLOCK (negative = debuff, positive = buff/statup)
    public int   BlockDeltaVal      { get; init; }
    // Non-zero when this effect changes PHYSICAL_CRITICAL (negative = debuff, positive = buff/statup)
    public int   PhysCritDeltaVal         { get; init; }
    // Non-zero when this effect changes MAGICAL_CRITICAL (negative = debuff, positive = buff/statup)
    public int   MagicCritDeltaVal        { get; init; }
    // Non-zero when this buff increases PHYSICAL_CRITICAL_RESIST (positive = more P-crit resist)
    public int   PhysCritResistDeltaVal   { get; init; }
    // Non-zero when this buff increases MAGICAL_CRITICAL_RESIST (positive = more M-crit resist)
    public int   MagicCritResistDeltaVal  { get; init; }
    // Non-zero when this buff increases PHYSICAL_CRITICAL_DAMAGE_REDUCE (strike fortitude)
    public int   StrikeFortitudeDeltaVal  { get; init; }
    // Non-zero when this buff increases MAGICAL_CRITICAL_DAMAGE_REDUCE (spell fortitude)
    public int   SpellFortitudeDeltaVal   { get; init; }
    // Non-zero when this effect changes BOOST_CASTING_TIME (negative = debuff/slower, positive = buff/faster)
    public int   CastTimeDeltaVal         { get; init; }
    // Non-zero when this effect changes CONCENTRATION (negative = debuff, positive = buff/statup)
    public int   ConcentrationDeltaVal    { get; init; }
    // Non-zero when this buff increases MAGIC_SKILL_BOOST_RESIST (magic suppression)
    public int   MagicSuppressionDeltaVal { get; init; }
    // Non-zero when this buff increases PHYSICAL_DEFENSE via statup (positive = more pdef)
    public int   PdefStatUpDeltaVal { get; init; }
    // Non-zero when this effect changes MAGICAL_DEFEND (negative = debuff, positive = statup buff)
    public int   MagicDefDeltaVal   { get; init; }
    // Non-zero when this buff increases PHYSICAL_ATTACK via statup (positive = more P-attack)
    public int   PatkStatUpDeltaVal     { get; init; }
    // Non-zero when this buff increases MAGICAL_ATTACK via statup (positive = more M-attack)
    public int   MagicAtkStatUpDeltaVal { get; init; }
    // Non-zero when this buff increases EVASION via statup (positive = harder to hit)
    public int   EvasionStatUpDeltaVal  { get; init; }
    // Non-zero when this buff increases MAGICAL_RESIST via statup (positive = more magic resist)
    public int   MResistStatUpDeltaVal  { get; init; }
    // Non-zero when this buff reduces ATTACK_SPEED ms via statup (negative = faster attacks)
    public int   AtkSpeedStatUpDeltaVal { get; init; }
    // Non-zero when this buff increases movement speed (positive percent, e.g. 30 = +30%)
    public int   SpeedStatUpPct         { get; init; }
    // MovementSpeed before this speed buff was applied — used to restore on expiry
    public float PreBuffMovSpeed        { get; init; }

    // Stack group name from the skill template's stack="..." attribute.
    // "NONE" (default) means no stack. Signets use "SYSTEM_SKILL_SIGNET1".
    public string StackName { get; init; } = "NONE";

    public bool IsExpired   => DateTime.UtcNow >= Expiry;
    public int  RemainingMs => IsExpired ? 0 : (int)Math.Min((Expiry - DateTime.UtcNow).TotalMilliseconds, int.MaxValue);
}
