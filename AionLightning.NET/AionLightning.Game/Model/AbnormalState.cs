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

    // Non-zero when this debuff reduces fly speed (e.g. Meteor Strike: -50 = halve fly speed)
    public int   FlySpeedDebuffPct    { get; init; }
    // Target's BonusFlySpeedPct before this debuff was applied — used to restore on expiry
    public int   PreDebuffFlySpeedPct { get; init; }

    // Non-zero when this buff increases fly speed (M351: FLY_SPEED PERCENT statup; e.g. Flyover Reconnaisance +33%)
    public int   FlySpeedStatUpPct    { get; init; }
    // Target's BonusFlySpeedPct before this buff was applied — used to restore on expiry
    public int   PreBuffFlySpeedPct   { get; init; }

    // Non-zero when this debuff blinds the target: each physical attack misses with this % chance.
    public int BlindDodgePct { get; init; }
    // Non-zero when this effect changes HEAL_SKILL_DEBOOST (negative = receive less healing, positive = receive more).
    public int HealReceivedPctDelta { get; init; }

    // True when this buff suppresses death penalty (soul sickness) — set from <noresurrectpenalty> element.
    public bool IsNoDeathPenalty { get; init; }

    // Non-zero when this buff changes HP regen rate (positive = % faster regen, e.g. 20 = +20% HP regen per tick).
    public int RegenHpPctDeltaVal { get; init; }
    // Non-zero when this buff changes MP regen rate (positive = % faster regen, e.g. 20 = +20% MP regen per tick).
    public int RegenMpPctDeltaVal { get; init; }
    // Non-zero when this buff changes FP regen rate (positive = % faster FP regen, e.g. 25 = +25% FP regen per tick).
    public int RegenFpPctDeltaVal { get; init; }
    // Non-zero when this buff adds flat HP per regen tick (M352: REGEN_HP ADD; e.g. Boost HP I +4, III +20).
    public int RegenHpAddDeltaVal { get; init; }
    // Non-zero when this buff adds flat MP per regen tick (M352: REGEN_MP ADD; e.g. Breath of Nature I +20, III +35).
    public int RegenMpAddDeltaVal { get; init; }

    // Non-zero when this buff increases death rank points gain (DR_BOOST ADD: e.g. 20 = +20% DR per kill).
    public int DRBoostDeltaVal { get; init; }
    // Non-zero when this buff increases AP gain (AP_BOOST ADD: e.g. 20 = +20% AP reward per kill).
    public int APBoostDeltaVal { get; init; }
    // Non-zero when this buff increases item drop rate (BOOST_DROP_RATE ADD; value is in per-mille: 10000 = +100%).
    public int BoostDropRateDeltaVal { get; init; }
    // Non-zero when this buff increases CC resist (ABNORMAL_RESISTANCE_ALL ADD; 0–10000 scale, where 10000 = 100% CC immune).
    public int CcResistAllDeltaVal { get; init; }
    // Non-zero when this buff changes hate generation rate (BOOST_HATE PERCENT; positive = more hate, negative = less).
    public int BoostHatePctDeltaVal { get; init; }
    // Non-zero when this buff increases MaxFp by percent (FLY_TIME PERCENT; e.g. 400 = +400% base MaxFp → effective MaxFp = base * 5).
    public int FlyTimePctDeltaVal { get; init; }
    // Non-zero when this buff increases MaxFp by a flat amount (M354: FLY_TIME ADD; e.g. GM's Armor +120, consumable scrolls +15).
    public int FlyTimeAddDeltaVal { get; init; }
    // Non-zero when this buff increases HEAL_SKILL_BOOST by percent (onetimeboostheal; e.g. 100 = +100% healing output).
    public int HealSkillBoostPct { get; init; }
    // Non-zero when this buff modifies skill MP cost (boostskillcost; e.g. 100 = free, 50 = half, -10 = 10% more expensive).
    public int BoostSkillCostPct { get; init; }
    // Non-zero when this debuff auto-revives the target at bind point on death (resurrectbase); value = revival skill_id (e.g. 8293).
    public int ResurrectBaseSkillId { get; init; }
    // M380: dispel category from the debuff's skill template (DEBUFF_PHYSICAL, DEBUFF_MENTAL, ALL, NONE, ...).
    // Determines which dispel effects can remove this debuff (physical-only, mental-only, or all-category cleanse).
    public string DispelCategory  { get; init; } = "NONE";
    // M380: minimum dispel_level a cleansing skill must carry to remove this debuff (Java req_dispel_level; usually 0 or 1).
    public int    ReqDispelLevel  { get; init; }
    // Non-zero when this buff increases solo-kill XP gain (BOOST_HUNTING_XP_RATE ADD; e.g. 30 = +30% solo XP per kill).
    public int HuntingXpBoostPct { get; init; }
    // Non-zero when this buff increases group-kill XP gain (BOOST_GROUP_HUNTING_XP_RATE ADD; e.g. 30 = +30% group XP per kill).
    public int GroupHuntingXpBoostPct { get; init; }

    // M340: PvP attack/defend ratio ADD deltas (Java ADD scale; /1000f multiplier applied in PvP damage path).
    public int PvpAtkRatioDelta { get; init; }
    public int PvpDefRatioDelta { get; init; }

    // M339: elemental resistance ADD deltas (Java scale; 1250 = 100% immune to that element).
    public int FireResistDelta  { get; init; }
    public int WaterResistDelta { get; init; }
    public int WindResistDelta  { get; init; }
    public int EarthResistDelta { get; init; }

    // M338: per-CC-type resistance deltas (Java 0–1000 scale; 1000 = 100% immune to that CC type).
    // Positive = buff; accumulated in Creature.StunResist etc. via ApplyEffectDeltas.
    public int StunResistDelta       { get; init; }
    public int StumbleResistDelta    { get; init; }
    public int StaggerResistDelta    { get; init; }
    public int SpinResistDelta       { get; init; }
    public int SleepResistDelta      { get; init; }
    public int FearResistDelta       { get; init; }
    public int OpenAerialResistDelta { get; init; }
    public int RootResistDelta       { get; init; }
    public int SnareResistDelta      { get; init; }

    // M334: one-time crit boost — charges consumed when casting a skill; 0 when not a onetimeboostskillcritical buff.
    public int  OnetimeCritCountRemaining { get; set; }
    // Flat crit-rating ADD applied for non-percent case (e.g. 1000 = +1000 crit rating for next N casts).
    public int  OnetimeCritBoostFlat      { get; init; }
    // Direct crit-% boost applied for percent=true case (e.g. 70 = +70% crit chance for next N casts).
    public int  OnetimeCritBoostPct       { get; init; }

    // M334: one-time atk boost — charges consumed when casting a matching skill type; 0 when not a onetimeboostskillattack buff.
    public int  OnetimeAtkCountRemaining  { get; set; }
    // Percent damage boost (e.g. 30 = +30% damage for next N casts).
    public int  OnetimeAtkBoostPct        { get; init; }
    // True = applies to PHYSICAL skill casts; false = MAGICAL.
    public bool OnetimeAtkBoostIsPhysical { get; init; }

    // True when the skill has a <sanctuary> element — this buff cannot be removed by dispel or healing potions.
    public bool IsSanctuary { get; init; }

    // Mutable hit counter for alwaysblock/alwaysdodge — decremented per hit; buff removed when it reaches 0.
    public int HitCountRemaining { get; set; }

    // M360: counter for alwaysresist charges — decremented each time an incoming magical attack is auto-resisted.
    // Buff removed when count reaches 0 (follows AlwaysBlock/Dodge/Parry pattern but for magical skills).
    public int AlwaysResistCountRemaining { get; set; }

    // M359: damage-absorbing shield (<shield> effect element; Java ShieldEffect shieldType=2).
    // ShieldHitValue: max per-hit absorb at this skill level (0 when not a shield buff).
    // ShieldPoolRemaining: mutable total pool; decremented as damage is absorbed; buff removed when reaches 0.
    // IsShieldPercent: when true, absorbs ShieldHitValue% of incoming damage instead of flat cap.
    public int  ShieldHitValue      { get; init; }
    public bool IsShieldPercent     { get; init; }
    public int  ShieldPoolRemaining { get; set; }

    // MpShieldHitValue: max per-hit damage absorbed (= MP drained) at this skill level (0 when not an mpshield buff).
    // MpShieldPoolRemaining: mutable total pool; decremented as damage is absorbed; buff removed when reaches 0.
    // IsMpShieldPercent: when true, absorbs MpShieldHitValue% of incoming damage.
    public int  MpShieldHitValue      { get; init; }
    public bool IsMpShieldPercent     { get; init; }
    public int  MpShieldPoolRemaining { get; set; }

    public bool IsExpired   => DateTime.UtcNow >= Expiry;
    public int  RemainingMs => IsExpired ? 0 : (int)Math.Min((Expiry - DateTime.UtcNow).TotalMilliseconds, int.MaxValue);

    // EffectTickScheduler slot handles owned by this effect (DoT/HoT ticks registered against it);
    // null until at least one tick is registered. Used to cancel outstanding ticks when the effect is removed.
    internal List<int>? TickHandles { get; set; }
}
