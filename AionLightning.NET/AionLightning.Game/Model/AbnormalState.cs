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

    public bool IsExpired   => DateTime.UtcNow >= Expiry;
    public int  RemainingMs => IsExpired ? 0 : (int)Math.Min((Expiry - DateTime.UtcNow).TotalMilliseconds, int.MaxValue);
}
