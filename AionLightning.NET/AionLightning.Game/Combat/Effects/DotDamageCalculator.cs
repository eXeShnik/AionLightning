using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4b: pure, unit-testable extraction of CM_CASTSPELL's DoT (bleed/poison/disease/spellatk) per-tick
/// damage computation (previously duplicated inline at 4 call sites: AoE/ground-target, splash, the
/// main single-target DoT block, and the skill-launcher/child-skill block). BEHAVIOR-IDENTICAL
/// relocation — every formula below is copied verbatim from the original packet handler.
///
/// The 4 original sites are NOT identical: sites 1 (AoE) and 2 (splash) apply a
/// <see cref="Player.PassiveBonusSpellAttackPct"/> multiplier in the spellatk branch and site 4
/// (launcher) additionally skips elemental-resist mitigation entirely in the non-spellatk branch.
/// Sites 3 (main single-target) and 4 (launcher) historically omit the passive-spell-attack bonus,
/// and site 4 omits elemental resist; these divergences are preserved verbatim via the
/// <paramref name="applyPassiveSpellAttackBonus"/>/<paramref name="applyElementalResist"/> flags and
/// are flagged as suspected latent inconsistencies for a later faithful-to-Java review — do NOT unify
/// without checking the Java skillengine.
/// </summary>
public static class DotDamageCalculator
{
    public static int ComputePerTick(
        SkillDotInfo dot, Player effector, Creature target, int level,
        bool applyPassiveSpellAttackBonus, bool applyElementalResist)
    {
        int rawDot = dot.BaseValue + dot.Delta * level;

        if (dot.DotType == "spellatk")
        {
            int mAtkDot = 100 + effector.MainHandMagicalAtk + effector.BonusMagicAtk + effector.MagicAtkDebuffDelta + effector.MagicAtkStatUpDelta;
            int dotSupp = target is Player dotPvp ? dotPvp.BonusMagicSuppression + dotPvp.MagicSuppressionDelta
                        : target is Npc dotNpc ? (dotNpc.Template.Stats?.MBResist ?? 0) : 0;
            float dotMb = 1.0f + Math.Max(0, effector.BonusMagicBoost + effector.MagicBoostDelta - dotSupp) / 1000f;
            int dotRaw = (int)((mAtkDot + rawDot) * dotMb);
            if (applyPassiveSpellAttackBonus && effector.PassiveBonusSpellAttackPct > 0)
                dotRaw = (int)(dotRaw * (1f + effector.PassiveBonusSpellAttackPct / 100f));
            int dotDef = target is Player dotDefPvp ? dotDefPvp.MagicDefense + dotDefPvp.MagicDefDelta
                        : target is Npc dotDefNpc ? (dotDefNpc.Template.Stats?.MBResist ?? 0) : 0;
            return Math.Max(1, dotDef > 0 ? dotRaw * 1000 / (1000 + dotDef) : dotRaw);
        }

        if (!applyElementalResist)
            return Math.Max(1, rawDot);

        // M343: elemental resistance for bleed/poison/disease DoT ticks
        int elemResist = ElementalResist.Get(target, dot.Element);
        return elemResist > 0
            ? Math.Max(1, (int)(rawDot * (1f - elemResist / 1250f)))
            : Math.Max(1, rawDot);
    }
}
