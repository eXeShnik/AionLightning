using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4d: pure, unit-testable extraction of CM_CASTSPELL's enemy-debuff stat/CC/speed delta computation
/// (previously inline locals just before the debuff's `new AbnormalState`). BEHAVIOR-IDENTICAL
/// relocation — every formula, cast, and `is Player` percent-of-target-base gate below is copied
/// verbatim from the original packet handler. Do NOT "clean up" or reorder the arithmetic here.
/// </summary>
public static class DebuffEffectCalculator
{
    public static DebuffDeltaSet Compute(SkillTemplate template, Creature target, int level)
    {
        var set = new DebuffDeltaSet();

        set.CcFlags = template.CcFlags;

        int  snareSpeedPct        = template.Effects?.SnareSpeedPct        ?? 0;
        int  statdownSpeedPct     = template.Effects?.StatdownSpeedPct     ?? 0;
        int  combinedMovSpeedPct  = snareSpeedPct + statdownSpeedPct;
        int  statdownFlySpeedPct  = template.Effects?.StatdownFlySpeedPct  ?? 0;
        int  slowAtkPct           = (template.Effects?.SlowAttackSpeedPct ?? 0)
                                  + (template.Effects?.StatdownAtkSpeedPct ?? 0); // M350
        int  pdefDelta            = template.Effects?.PdefAddDelta       ?? 0;
        int  mresistDelta         = template.Effects?.MResistAddDelta    ?? 0;
        int  patkDelta            = template.Effects?.PhysAtkAddDelta    ?? 0;
        int  evasionDelta         = template.Effects?.EvasionAddDelta    ?? 0;
        int  maxHpDelta           = template.Effects?.MaxHpAddDelta      ?? 0;
        int  magicAtkDelta        = template.Effects?.MagicAtkAddDelta   ?? 0;
        int  atkSpdDelta          = template.Effects?.AtkSpeedAddDelta   ?? 0;
        int  maxMpDelta           = template.Effects?.MaxMpAddDelta        ?? 0;
        int  maxHpPctDelta        = template.Effects?.MaxHpPercentDelta    ?? 0;
        int  maxMpPctDelta        = template.Effects?.MaxMpPercentDelta    ?? 0;
        if (maxHpPctDelta != 0) maxHpDelta += target.MaxHp * maxHpPctDelta / 100;
        if (maxMpPctDelta != 0) maxMpDelta += target.MaxMp * maxMpPctDelta / 100;
        // M322: PERCENT pdef/mresist debuffs — convert to flat delta against target's current stat
        int  pdefPctDebuff    = template.Effects?.PdefPercentDebuff    ?? 0;
        int  mresistPctDebuff = template.Effects?.MResistPercentDebuff ?? 0;
        if (pdefPctDebuff    != 0 && target is Player pdefPctTgt)    pdefDelta    += pdefPctTgt.PhysicalDefense  * pdefPctDebuff    / 100;
        if (mresistPctDebuff != 0 && target is Player mresistPctTgt) mresistDelta += mresistPctTgt.BonusMagicResist * mresistPctDebuff / 100;
        // M326: PERCENT patk/evasion/matk debuffs — convert to flat delta against target's current stat
        int  patkPctDebuff     = template.Effects?.PatkPercentDebuff     ?? 0;
        int  evasionPctDebuff  = template.Effects?.EvasionPercentDebuff  ?? 0;
        int  magicAtkPctDebuff = template.Effects?.MagicAtkPercentDebuff ?? 0;
        if (patkPctDebuff    != 0 && target is Player patkPctTgt)
            patkDelta    += (patkPctTgt.BasePhysicalAttack + (patkPctTgt.MainHandMinDmg + patkPctTgt.MainHandMaxDmg) / 2 + patkPctTgt.BonusPhysicalAtk) * patkPctDebuff / 100;
        if (evasionPctDebuff != 0 && target is Player evasionPctTgt)
            evasionDelta += (evasionPctTgt.BaseEvasion + evasionPctTgt.BonusEvasion) * evasionPctDebuff / 100;
        if (magicAtkPctDebuff!= 0 && target is Player matkPctTgt)
            magicAtkDelta+= (matkPctTgt.MainHandMagicalAtk + matkPctTgt.BonusMagicAtk) * magicAtkPctDebuff / 100;
        int  mBoostDebuffDelta     = template.Effects?.MagicBoostAddDelta   ?? 0;
        // M328: PERCENT BOOST_MAGICAL_SKILL debuff
        int  mBoostPctDebuff = template.Effects?.MagicBoostPctDebuff ?? 0;
        if (mBoostPctDebuff != 0 && target is Player mBoostPctTgt)
            mBoostDebuffDelta += mBoostPctTgt.BonusMagicBoost * mBoostPctDebuff / 100;
        int  physAccDelta          = template.Effects?.PhysAccAddDelta       ?? 0;
        // M327: PERCENT PHYSICAL_ACCURACY debuff — reduce phys acc by pct of target's current accuracy
        int  physAccPctDebuff = template.Effects?.PhysAccPercentDebuff ?? 0;
        if (physAccPctDebuff != 0 && target is Player physAccPctTgt)
            physAccDelta += (physAccPctTgt.BasePhysicalAccuracy + physAccPctTgt.BonusPhysicalAccuracy) * physAccPctDebuff / 100;
        int  magicAccDelta         = template.Effects?.MagicAccAddDelta      ?? 0;
        int  parryDelta            = template.Effects?.ParryAddDelta         ?? 0;
        int  blockDelta            = template.Effects?.BlockAddDelta         ?? 0;
        // M328: PERCENT BLOCK debuff — reduce block rating by pct of target's current block
        int  blockPctDebuff = template.Effects?.BlockPercentDebuff ?? 0;
        if (blockPctDebuff != 0 && target is Player blockPctTgt)
            blockDelta += (blockPctTgt.BaseBlock + blockPctTgt.BonusBlock) * blockPctDebuff / 100;
        int  physCritDelta         = template.Effects?.PhysCritAddDelta         ?? 0;
        int  magicCritDelta        = template.Effects?.MagicCritAddDelta        ?? 0;
        int  physCritResistDelta   = template.Effects?.PhysCritResistAddDelta    ?? 0;
        int  magicCritResistDelta  = template.Effects?.MagicCritResistAddDelta   ?? 0;
        int  strikeFortitudeDelta  = template.Effects?.StrikeFortitudeAddDelta   ?? 0;
        int  spellFortitudeDelta   = template.Effects?.SpellFortitudeAddDelta    ?? 0;
        int  castTimeDelta         = template.Effects?.CastTimeAddDelta           ?? 0;
        int  concentrationDelta    = template.Effects?.ConcentrationAddDelta      ?? 0;
        int  magicSuppressionDelta = template.Effects?.MagicSuppressionAddDelta   ?? 0;
        int  magicDefDelta         = template.Effects?.MagicDefAddDelta            ?? 0;
        int  blindDodgePct         = template.Effects?.BlindDodgePct               ?? 0;
        int  healDeboostPct        = template.Effects?.HealDeboostPct              ?? 0;
        // M353: elemental resist ADD debuffs — ScanStatupAddValue sums statdown negative values; 271 entries (Agonizing Slash, etc.)
        int  fireResistDebuff  = template.Effects?.FireResistDelta  ?? 0;
        int  waterResistDebuff = template.Effects?.WaterResistDelta ?? 0;
        int  windResistDebuff  = template.Effects?.WindResistDelta  ?? 0;
        int  earthResistDebuff = template.Effects?.EarthResistDelta ?? 0;
        // M355: elemental resist PERCENT debuffs — 40 entries (Flight: Enervating Bind I-III, etc.; -100 = remove 100% of current resist)
        int  fireResistPct  = template.Effects?.FireResistPctDebuff  ?? 0;
        int  waterResistPct = template.Effects?.WaterResistPctDebuff ?? 0;
        int  windResistPct  = template.Effects?.WindResistPctDebuff  ?? 0;
        int  earthResistPct = template.Effects?.EarthResistPctDebuff ?? 0;
        if (fireResistPct  != 0 && target is Player frPctTgt) fireResistDebuff  += frPctTgt.FireResist  * fireResistPct  / 100;
        if (waterResistPct != 0 && target is Player wrPctTgt) waterResistDebuff += wrPctTgt.WaterResist * waterResistPct / 100;
        if (windResistPct  != 0 && target is Player wiPctTgt) windResistDebuff  += wiPctTgt.WindResist  * windResistPct  / 100;
        if (earthResistPct != 0 && target is Player erPctTgt) earthResistDebuff += erPctTgt.EarthResist * earthResistPct / 100;
        // M356: CC resistance ADD debuffs — ScanStatupAddValue scans statdown too; negative values reduce target's CC resistance
        int  sleepResistDebuff  = template.Effects?.SleepResistDelta      ?? 0;
        int  rootResistDebuff   = template.Effects?.RootResistDelta       ?? 0;
        int  stunResistDebuff   = template.Effects?.StunResistDelta       ?? 0;
        int  stumbleResistDebuff= template.Effects?.StumbleResistDelta    ?? 0;
        int  staggerResistDebuff= template.Effects?.StaggerResistDelta    ?? 0;
        int  spinResistDebuff   = template.Effects?.SpinResistDelta       ?? 0;
        int  snareResistDebuff  = template.Effects?.SnareResistDelta      ?? 0;
        int  fearResistDebuff   = template.Effects?.FearResistDelta       ?? 0;
        int  openAerialResistDb = template.Effects?.OpenAerialResistDelta ?? 0;
        // M379: resurrectbase — auto-revive target at bind point on death (Chain of Suffering I-VII)
        int  resurrectBaseSkillId = template.Effects?.ResurrectBaseSkillId ?? 0;
        // M380: carry dispel_category + req_dispel_level so targeted cleanse effects (physical/mental) can filter correctly
        string debuffDispelCategory = template.DispelCategory;
        int    debuffReqDispelLevel  = template.ReqDispelLevel;

        set.MovSpeedPct             = combinedMovSpeedPct;
        set.AttackSpeedPct          = slowAtkPct;
        set.FlySpeedDebuffPct       = statdownFlySpeedPct;
        set.PdefDelta               = pdefDelta;
        set.MResistDelta            = mresistDelta;
        set.PatkDelta               = patkDelta;
        set.EvasionDelta            = evasionDelta;
        set.MaxHpDelta              = maxHpDelta;
        set.MagicAtkDelta           = magicAtkDelta;
        set.AtkSpeedDelta           = atkSpdDelta;
        set.MaxMpDelta              = maxMpDelta;
        set.MagicBoostDeltaVal      = mBoostDebuffDelta;
        set.PhysAccDeltaVal         = physAccDelta;
        set.MagicAccDeltaVal        = magicAccDelta;
        set.ParryDeltaVal           = parryDelta;
        set.BlockDeltaVal           = blockDelta;
        set.PhysCritDeltaVal        = physCritDelta;
        set.MagicCritDeltaVal       = magicCritDelta;
        set.PhysCritResistDeltaVal  = physCritResistDelta;
        set.MagicCritResistDeltaVal = magicCritResistDelta;
        set.StrikeFortitudeDeltaVal = strikeFortitudeDelta;
        set.SpellFortitudeDeltaVal  = spellFortitudeDelta;
        set.CastTimeDeltaVal        = castTimeDelta;
        set.ConcentrationDeltaVal   = concentrationDelta;
        set.MagicSuppressionDeltaVal = magicSuppressionDelta;
        set.MagicDefDeltaVal        = magicDefDelta;
        set.BlindDodgePct           = blindDodgePct;
        set.HealReceivedPctDelta    = healDeboostPct;
        set.FireResistDelta         = fireResistDebuff;
        set.WaterResistDelta        = waterResistDebuff;
        set.WindResistDelta         = windResistDebuff;
        set.EarthResistDelta        = earthResistDebuff;
        set.SleepResistDelta        = sleepResistDebuff;
        set.RootResistDelta         = rootResistDebuff;
        set.StunResistDelta         = stunResistDebuff;
        set.StumbleResistDelta      = stumbleResistDebuff;
        set.StaggerResistDelta      = staggerResistDebuff;
        set.SpinResistDelta         = spinResistDebuff;
        set.SnareResistDelta        = snareResistDebuff;
        set.FearResistDelta         = fearResistDebuff;
        set.OpenAerialResistDelta   = openAerialResistDb;
        set.ResurrectBaseSkillId    = resurrectBaseSkillId;
        set.DispelCategory          = debuffDispelCategory;
        set.ReqDispelLevel          = debuffReqDispelLevel;

        return set;
    }
}
