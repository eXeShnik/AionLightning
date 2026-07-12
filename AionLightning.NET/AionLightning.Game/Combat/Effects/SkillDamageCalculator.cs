using AionLightning.Game.Model;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4e: pure, unit-testable extraction of CM_CASTSPELL's direct-damage math (previously duplicated
/// inline at 6 call sites: skill-launcher/child-skill expiry damage, caster-centered AoE, ground-AoE,
/// the main single-target hit, delaydamage, and splash AoE). BEHAVIOR-IDENTICAL relocation — every
/// formula below is copied verbatim from the original packet handler.
///
/// The 6 original sites are NOT identical — they diverge on whether the passive spell-attack bonus
/// applies, whether crit uses the fortitude coefficient or a fixed 1.5x, whether NPC level-diff /
/// PvP reduction applies, and whether elemental resist applies. Every divergence is preserved
/// verbatim via the bool flag parameters below rather than unified silently; see the call sites in
/// CM_CASTSPELL.cs for the exact flag values used per site.
///
/// Crit is ROLLED by the caller (the RNG stays at the call site) — <see cref="ApplyCrit"/> only
/// applies the already-decided outcome. Likewise resist/dodge rolls, onetime-charge consumption, and
/// all packet sends / state mutation stay at the call sites.
/// </summary>
public static class SkillDamageCalculator
{
    /// <summary>
    /// Attack-stat term: magical = (100 + MainHandMagicalAtk + BonusMagicAtk + MagicAtkDebuffDelta +
    /// MagicAtkStatUpDelta + baseValue) * magicBoostMult, with magicBoostMult driven by the effector's
    /// bonus magic boost less the target's magic suppression (Player suppression fields, or Npc
    /// MBResist). Physical = BasePhysicalAttack + avg(MainHandMinDmg, MainHandMaxDmg) + BonusPhysicalAtk
    /// + PatkStatUpDelta + baseValue (no boost term). The passive spell-attack-bonus multiplier only
    /// ever applies on the magical branch (matches every original call site, which either gates the
    /// check on spellIsMagical explicitly or nests it inside an `if (spellIsMagical)` block). The
    /// onetime-attack-charge percent bump applies unconditionally afterward, regardless of school.
    /// </summary>
    public static int ComputeRaw(Player effector, Creature target, int baseValue, bool isMagical,
        bool applyPassiveSpellAttackBonus, int onetimeAtkPct)
    {
        int raw;
        if (isMagical)
        {
            int mAtk = 100 + effector.MainHandMagicalAtk + effector.BonusMagicAtk
                           + effector.MagicAtkDebuffDelta + effector.MagicAtkStatUpDelta;
            int suppress = target is Player pvpSupp ? pvpSupp.BonusMagicSuppression + pvpSupp.MagicSuppressionDelta
                         : target is Npc npcSupp ? (npcSupp.Template.Stats?.MBResist ?? 0) : 0;
            float mbMult = 1.0f + Math.Max(0, effector.BonusMagicBoost + effector.MagicBoostDelta - suppress) / 1000f;
            raw = (int)((mAtk + baseValue) * mbMult);
            if (applyPassiveSpellAttackBonus && effector.PassiveBonusSpellAttackPct > 0)
                raw = (int)(raw * (1f + effector.PassiveBonusSpellAttackPct / 100f));
        }
        else
        {
            int pAtk = effector.BasePhysicalAttack + (effector.MainHandMinDmg + effector.MainHandMaxDmg) / 2
                     + effector.BonusPhysicalAtk + effector.PatkStatUpDelta;
            raw = pAtk + baseValue;
        }
        if (onetimeAtkPct != 0) raw = Math.Max(1, raw * (100 + onetimeAtkPct) / 100);
        return raw;
    }

    /// <summary>
    /// Piecewise crit-rate curve shared by magical and physical crit (Java calculateMagical/
    /// PhysicalCriticalRate): &lt;=440 -&gt; *0.1, &lt;=600 -&gt; 44+(r-440)*0.05, else 52+(r-600)*0.02,
    /// then + onetimeCritPct capped at 100. Caller pre-nets any crit-resist into <paramref name="critRating"/>
    /// before calling (sites without crit-resist just pass the raw summed rating). The RNG roll against
    /// this rate happens at the call site, not here.
    /// </summary>
    public static double ComputeCritRate(int critRating, int onetimeCritPct)
    {
        double rate = critRating <= 440 ? critRating * 0.1
                    : critRating <= 600 ? 44.0 + (critRating - 440) * 0.05
                    : 52.0 + (critRating - 600) * 0.02;
        if (onetimeCritPct > 0) rate = Math.Min(100, rate + onetimeCritPct);
        return rate;
    }

    /// <summary>
    /// Applies a previously-rolled crit outcome. Fixed 1.5x coefficient when
    /// <paramref name="useFortitudeCoeff"/> is false (S2/S6); otherwise
    /// max(1, 1.5 - round(fortitude/1000)) using the target's spell (magical) or strike (physical)
    /// fortitude — 0 for non-Player targets, matching the original ternaries. No-op when
    /// <paramref name="didCrit"/> is false.
    /// </summary>
    public static int ApplyCrit(int raw, Creature target, bool isMagical, bool didCrit, bool useFortitudeCoeff)
    {
        if (!didCrit) return raw;
        if (!useFortitudeCoeff) return (int)(raw * 1.5f);

        int fort = target is Player pvpFort
            ? (isMagical ? pvpFort.BonusSpellFortitude + pvpFort.SpellFortitudeDelta
                         : pvpFort.BonusStrikeFortitude + pvpFort.StrikeFortitudeDelta)
            : 0;
        float coeff = Math.Max(1.0f, 1.5f - (float)Math.Round(fort / 1000.0));
        return (int)(raw * coeff);
    }

    /// <summary>
    /// Java StatFunctions.adjustDamages: Npc targets take the NpcLevelDiffMod reduction (flagged);
    /// Player targets take a flat 50% PvP reduction followed by the attacker/defender PvP-ratio
    /// adjustment (flagged). Both flags are no-ops when the target's runtime type doesn't match the
    /// branch (e.g. applyPvp is irrelevant for an Npc target).
    /// </summary>
    public static int ApplyLevelDiffAndPvp(int raw, Player effector, Creature target,
        bool applyNpcLevelDiffMod, bool applyPvp)
    {
        if (target is Npc npc)
        {
            if (!applyNpcLevelDiffMod) return raw;
            float lvlMod = CombatMath.NpcLevelDiffMod(npc.Level - effector.Level);
            return lvlMod > 0f ? Math.Max(1, (int)(raw * (1f - lvlMod))) : raw;
        }
        if (target is Player pvpTarget && applyPvp)
        {
            raw = Math.Max(1, raw / 2);
            int net = effector.PvpAtkRatio - pvpTarget.PvpDefRatio;
            if (net != 0) raw = Math.Max(1, (int)(raw * (1f + net * 0.001f)));
        }
        return raw;
    }

    /// <summary>
    /// Defense mitigation (`def>0 ? Max(1, raw*1000/(1000+def)) : raw`, def = magic/physical defense
    /// per <paramref name="isMagical"/>, read from Player fields or Npc template stats), then a
    /// noreduce override that replaces the mitigated value entirely (flagged), then elemental resist
    /// (flagged, magical only) applied to whatever damage remains after the noreduce step — matching
    /// every original call site where the elemental-resist check is explicitly gated on "no noreduce
    /// effect present".
    /// </summary>
    public static int ApplyDefenseAndResist(int raw, Creature target, bool isMagical,
        bool hasNoReduce, int noReduceValue, bool noReduceIsPercent,
        string element, bool applyElementalResist)
    {
        int def = target is Player pvpTarget
                ? (isMagical ? pvpTarget.MagicDefense + pvpTarget.MagicDefDelta
                             : pvpTarget.PhysicalDefense + pvpTarget.PdefDebuffDelta + pvpTarget.PdefStatUpDelta)
                : target is Npc npcTarget
                ? (isMagical ? (npcTarget.Template.Stats?.MBResist ?? 0) : (npcTarget.Template.Stats?.PDef ?? 0))
                : 0;
        int damage = def > 0 ? Math.Max(1, raw * 1000 / (1000 + def)) : raw;

        if (hasNoReduce)
            return noReduceIsPercent ? Math.Max(1, target.MaxHp * noReduceValue / 100) : Math.Max(1, noReduceValue);

        if (applyElementalResist && isMagical)
        {
            int elemResist = ElementalResist.Get(target, element);
            if (elemResist > 0)
                damage = Math.Max(1, (int)(damage * (1f - elemResist / 1250f)));
        }
        return damage;
    }
}
