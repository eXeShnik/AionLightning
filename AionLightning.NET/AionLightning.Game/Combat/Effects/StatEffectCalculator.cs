using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4a: pure, unit-testable extraction of CM_CASTSPELL's buff stat-delta computation
/// (previously inline locals at ~lines 672-820). BEHAVIOR-IDENTICAL relocation — every
/// formula, cast, and `is Player` percent-of-base gate below is copied verbatim from the
/// original packet handler. Do NOT "clean up" or reorder the arithmetic here.
/// </summary>
public static class StatEffectCalculator
{
    public static StatDeltaSet Compute(SkillTemplate template, Creature target, int level)
    {
        var stat = new StatDeltaSet();

        stat.MaxHpDelta = template.Effects?.MaxHpStatUpDelta ?? 0;
        // M349: PERCENT MAXHP buffs from statup/statboost (Second Wind, Improved Stamina, Blessing of Health, etc.)
        int maxHpStatUpPct = template.Effects?.MaxHpPercentStatUpDelta ?? 0;
        if (maxHpStatUpPct != 0 && target is Player maxHpPctBuff)
            stat.MaxHpDelta += maxHpPctBuff.MaxHp * maxHpStatUpPct / 100;
        stat.MaxMpDelta = template.Effects?.MaxMpStatUpDelta ?? 0;
        // M349: PERCENT MAXMP buffs from statup/statboost
        int maxMpStatUpPct = template.Effects?.MaxMpPercentStatUpDelta ?? 0;
        if (maxMpStatUpPct != 0 && target is Player maxMpPctBuff)
            stat.MaxMpDelta += maxMpPctBuff.MaxMp * maxMpStatUpPct / 100;
        stat.MagicBoostDeltaVal = template.Effects?.MagicBoostStatUpDelta ?? 0;
        stat.HealBoostDeltaVal = template.Effects?.HealBoostStatUpDelta ?? 0;
        stat.PhysAccDeltaVal = template.Effects?.PhysAccStatUpDelta ?? 0;
        stat.MagicAccDeltaVal = template.Effects?.MagicAccStatUpDelta ?? 0;
        stat.ParryDeltaVal = template.Effects?.ParryStatUpDelta ?? 0;
        stat.BlockDeltaVal = template.Effects?.BlockStatUpDelta ?? 0;
        stat.PhysCritDeltaVal = template.Effects?.PhysCritStatUpDelta ?? 0;
        stat.MagicCritDeltaVal = template.Effects?.MagicCritStatUpDelta ?? 0;
        stat.PhysCritResistDeltaVal = template.Effects?.PhysCritResistStatUpDelta ?? 0;
        stat.MagicCritResistDeltaVal = template.Effects?.MagicCritResistStatUpDelta ?? 0;
        stat.StrikeFortitudeDeltaVal = template.Effects?.StrikeFortitudeStatUpDelta ?? 0;
        stat.SpellFortitudeDeltaVal = template.Effects?.SpellFortitudeStatUpDelta ?? 0;
        stat.CastTimeDeltaVal = (template.Effects?.CastTimeStatUpDelta ?? 0)
                               + (template.Effects?.BoostCastTimePctDelta ?? 0)
                               + (template.Effects?.CastTimeStatUpPctDelta ?? 0); // M348
        stat.ConcentrationDeltaVal = template.Effects?.ConcentrationStatUpDelta ?? 0;
        stat.MagicSuppressionDeltaVal = template.Effects?.MagicSuppressionStatUpDelta ?? 0;
        stat.PdefStatUpDeltaVal = template.Effects?.PdefStatUpDelta ?? 0;
        stat.MagicDefDeltaVal = template.Effects?.MagicDefStatUpDelta ?? 0;
        stat.PatkStatUpDeltaVal = template.Effects?.PhysAtkStatUpDelta ?? 0;
        stat.MagicAtkStatUpDeltaVal = template.Effects?.MagicAtkStatUpDelta ?? 0;
        stat.EvasionStatUpDeltaVal = template.Effects?.EvasionStatUpDelta ?? 0;
        stat.MResistStatUpDeltaVal = template.Effects?.MResistStatUpDelta ?? 0;
        stat.AtkSpeedStatUpDeltaVal = template.Effects?.AtkSpeedStatUpDelta ?? 0;
        stat.SpeedStatUpPct = template.Effects?.SpeedStatUpPct ?? 0;
        // M323: PERCENT PHYSICAL_ATTACK and MAGICAL_ATTACK buffs — convert to flat delta
        int patkStatUpPct = template.Effects?.PhysAtkStatUpPct ?? 0;
        int magicAtkStatUpPct = template.Effects?.MagicAtkStatUpPct ?? 0;
        if (patkStatUpPct != 0 && target is Player patkPctBuff)
        {
            int basePatk = patkPctBuff.BasePhysicalAttack
                           + (patkPctBuff.MainHandMinDmg + patkPctBuff.MainHandMaxDmg) / 2
                           + patkPctBuff.BonusPhysicalAtk;
            stat.PatkStatUpDeltaVal += basePatk * patkStatUpPct / 100;
        }
        if (magicAtkStatUpPct != 0 && target is Player matkPctBuff)
            stat.MagicAtkStatUpDeltaVal += (matkPctBuff.MainHandMagicalAtk + matkPctBuff.BonusMagicAtk) * magicAtkStatUpPct / 100;
        // M324: PERCENT PDEF and ATTACK_SPEED buffs
        int pdefStatUpPct = template.Effects?.PdefStatUpPct ?? 0;
        int atkSpdStatUpPct = template.Effects?.AtkSpeedStatUpPct ?? 0;
        if (pdefStatUpPct != 0 && target is Player pdefPctBuff)
            stat.PdefStatUpDeltaVal += pdefPctBuff.PhysicalDefense * pdefStatUpPct / 100;
        if (atkSpdStatUpPct != 0)
            stat.AtkSpeedStatUpDeltaVal += target.CurrentAttackSpeed * atkSpdStatUpPct / 100;
        // M325: PERCENT MAGICAL_RESIST and EVASION buffs
        int mresistStatUpPct = template.Effects?.MResistStatUpPct ?? 0;
        int evasionStatUpPct = template.Effects?.EvasionStatUpPct ?? 0;
        if (mresistStatUpPct != 0 && target is Player mresistPctBuff)
            stat.MResistStatUpDeltaVal += mresistPctBuff.BonusMagicResist * mresistStatUpPct / 100;
        if (evasionStatUpPct != 0 && target is Player evasionPctBuff)
            stat.EvasionStatUpDeltaVal += (evasionPctBuff.BaseEvasion + evasionPctBuff.BonusEvasion) * evasionStatUpPct / 100;
        // M327: PERCENT PHYSICAL_CRITICAL and PHYSICAL_ACCURACY buffs
        int physCritStatUpPct = template.Effects?.PhysCritStatUpPct ?? 0;
        int physAccStatUpPct = template.Effects?.PhysAccStatUpPct ?? 0;
        if (physCritStatUpPct != 0 && target is Player physCritPctBuff)
            stat.PhysCritDeltaVal += (physCritPctBuff.BaseCritRating + physCritPctBuff.BonusPhysicalCritical) * physCritStatUpPct / 100;
        if (physAccStatUpPct != 0 && target is Player physAccPctBuff)
            stat.PhysAccDeltaVal += (physAccPctBuff.BasePhysicalAccuracy + physAccPctBuff.BonusPhysicalAccuracy) * physAccStatUpPct / 100;
        // M328: PERCENT BOOST_MAGICAL_SKILL buff + BLOCK and PARRY PERCENT buffs
        int mBoostStatUpPct = template.Effects?.MagicBoostStatUpPct ?? 0;
        int blockStatUpPct = template.Effects?.BlockStatUpPct ?? 0;
        int parryStatUpPct = template.Effects?.ParryStatUpPct ?? 0;
        if (mBoostStatUpPct != 0 && target is Player mBoostPctBuff)
            stat.MagicBoostDeltaVal += mBoostPctBuff.BonusMagicBoost * mBoostStatUpPct / 100;
        if (blockStatUpPct != 0 && target is Player blockPctBuff)
            stat.BlockDeltaVal += (blockPctBuff.BaseBlock + blockPctBuff.BonusBlock) * blockStatUpPct / 100;
        if (parryStatUpPct != 0 && target is Player parryPctBuff)
            stat.ParryDeltaVal += (parryPctBuff.BaseParry + parryPctBuff.BonusParry) * parryStatUpPct / 100;
        // M329: PERCENT MAGICAL_DEFEND buff
        int magicDefStatUpPct = template.Effects?.MagicDefStatUpPct ?? 0;
        if (magicDefStatUpPct != 0 && target is Player magicDefPctBuff)
            stat.MagicDefDeltaVal += magicDefPctBuff.MagicDefense * magicDefStatUpPct / 100;
        // M330/M331: REGEN_HP/MP/FP PERCENT buffs — stored as pct delta, applied per-tick in RegenService
        stat.RegenHpPctDeltaVal = template.Effects?.RegenHpStatUpPct ?? 0;
        stat.RegenMpPctDeltaVal = template.Effects?.RegenMpStatUpPct ?? 0;
        stat.RegenFpPctDeltaVal = template.Effects?.RegenFpStatUpPct ?? 0;
        // M352: REGEN_HP/MP ADD buffs — flat HP/MP added per regen tick (Boost HP I-III, Breath of Nature I-III, etc.)
        stat.RegenHpAddDeltaVal = template.Effects?.RegenHpAddDelta ?? 0;
        stat.RegenMpAddDeltaVal = template.Effects?.RegenMpAddDelta ?? 0;
        // M332/M364: DR_BOOST, AP_BOOST, and BOOST_DROP_RATE ADD buffs
        stat.DRBoostDeltaVal = template.Effects?.DRBoostAddDelta ?? 0;
        stat.APBoostDeltaVal = template.Effects?.APBoostAddDelta ?? 0;
        stat.BoostDropRateDeltaVal = template.Effects?.BoostDropRateAddDelta ?? 0;
        // M333: ABNORMAL_RESISTANCE_ALL ADD buff and BOOST_HATE PERCENT buff
        stat.CcResistAllDeltaVal = template.Effects?.CcResistAllAddDelta ?? 0;
        stat.BoostHatePctDeltaVal = template.Effects?.BoostHateStatPct ?? 0;
        // M335: FLY_TIME PERCENT buff — increases player MaxFp by a percentage
        stat.FlyTimePctDeltaVal = template.Effects?.FlyTimeStatUpPct ?? 0;
        // M354: FLY_TIME ADD buff — increases player MaxFp by a flat amount (GM buffs, consumable scrolls)
        stat.FlyTimeAddDeltaVal = template.Effects?.FlyTimeAddDelta ?? 0;
        // M351: FLY_SPEED PERCENT buff from statup/statboost (e.g. Flyover Reconnaisance +33%, Charge +60%)
        stat.FlySpeedStatUpPct = template.Effects?.FlySpeedStatUpPct ?? 0;
        // M336: XP rate buffs — solo and group hunting XP boost
        stat.HuntingXpBoostPct = template.Effects?.HuntingXpBoostPct ?? 0;
        stat.GroupHuntingXpBoostPct = template.Effects?.GroupHuntingXpBoostPct ?? 0;
        // M337: onetimeboostheal — HEAL_SKILL_BOOST PERCENT timed buff (e.g. Blessed Shield +100%)
        stat.HealSkillBoostPct = template.Effects?.OnetimeBoostHealPct ?? 0;
        // M378: boostskillcost — skill MP cost modifier (e.g. Grace of Empyrean Lord: 100 = free)
        stat.BoostSkillCostPct = template.Effects?.BoostSkillCostPct ?? 0;
        // M340: PvP attack/defend ratio ADD buffs (Java ADD; /1000f applied in damage path)
        stat.PvpAtkRatioDelta = template.Effects?.PvpAtkRatioDelta ?? 0;
        stat.PvpDefRatioDelta = template.Effects?.PvpDefRatioDelta ?? 0;
        // M339: elemental resistance ADD buffs (scale: 1250 = 100% immune to that element)
        stat.FireResistDelta = template.Effects?.FireResistDelta ?? 0;
        stat.WaterResistDelta = template.Effects?.WaterResistDelta ?? 0;
        stat.WindResistDelta = template.Effects?.WindResistDelta ?? 0;
        stat.EarthResistDelta = template.Effects?.EarthResistDelta ?? 0;
        // M338: per-CC-type resistance ADD buffs (Java 0–1000 scale)
        stat.StunResistDelta = template.Effects?.StunResistDelta ?? 0;
        stat.StumbleResistDelta = template.Effects?.StumbleResistDelta ?? 0;
        stat.StaggerResistDelta = template.Effects?.StaggerResistDelta ?? 0;
        stat.SpinResistDelta = template.Effects?.SpinResistDelta ?? 0;
        stat.SleepResistDelta = template.Effects?.SleepResistDelta ?? 0;
        stat.FearResistDelta = template.Effects?.FearResistDelta ?? 0;
        stat.OpenAerialResistDelta = template.Effects?.OpenAerialResistDelta ?? 0;
        stat.RootResistDelta = template.Effects?.RootResistDelta ?? 0;
        stat.SnareResistDelta = template.Effects?.SnareResistDelta ?? 0;
        // M341: BUFF-path self-nerfing statdown elements — apply negative stat deltas to buff caster
        // Java: statdown inside a BUFF skill applies to the effector (caster), not the enemy target
        int pdefSelfNerfPct = template.Effects?.PdefPercentDebuff ?? 0;
        if (pdefSelfNerfPct != 0 && target is Player pdefNerfTgt)
            stat.PdefStatUpDeltaVal += pdefNerfTgt.PhysicalDefense * pdefSelfNerfPct / 100;
        stat.PdefStatUpDeltaVal += template.Effects?.PdefAddDelta ?? 0;
        int patkSelfNerfPct = template.Effects?.PatkPercentDebuff ?? 0;
        if (patkSelfNerfPct != 0 && target is Player patkNerfTgt)
            stat.PatkStatUpDeltaVal += (patkNerfTgt.BasePhysicalAttack + patkNerfTgt.BonusPhysicalAtk) * patkSelfNerfPct / 100;
        stat.PatkStatUpDeltaVal += template.Effects?.PhysAtkAddDelta ?? 0;
        int evasionSelfNerfPct = template.Effects?.EvasionPercentDebuff ?? 0;
        if (evasionSelfNerfPct != 0 && target is Player evNerfTgt)
            stat.EvasionStatUpDeltaVal += (evNerfTgt.BaseEvasion + evNerfTgt.BonusEvasion) * evasionSelfNerfPct / 100;
        stat.EvasionStatUpDeltaVal += template.Effects?.EvasionAddDelta ?? 0;
        stat.MResistStatUpDeltaVal += template.Effects?.MResistAddDelta ?? 0;
        stat.PhysAccDeltaVal += template.Effects?.PhysAccAddDelta ?? 0;
        stat.ConcentrationDeltaVal += template.Effects?.ConcentrationAddDelta ?? 0;
        stat.PhysCritDeltaVal += template.Effects?.PhysCritAddDelta ?? 0;
        // M350: BUFF-path statdown ATTACK_SPEED PERCENT self-nerf (Bravery of the Composed +70%)
        int atkSpdSelfNerfPct = template.Effects?.StatdownAtkSpeedPct ?? 0;
        if (atkSpdSelfNerfPct != 0)
            stat.AtkSpeedStatUpDeltaVal += target.CurrentAttackSpeed * atkSpdSelfNerfPct / 100;

        return stat;
    }
}
