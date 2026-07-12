using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4c: pure, unit-testable extraction of CM_CASTSPELL's HEAL / heal-over-time amount computation
/// (previously duplicated inline at the instant-heal (A1), AoE ally heal (A4), and BUFF-bundled heal
/// (A6) call sites, plus the two HoT tick bases (A2/A5)). BEHAVIOR-IDENTICAL relocation — every
/// formula below is copied verbatim from the original packet handler.
///
/// Java parity: HealOverTimeEffect scales the tick base by <c>delta*skillLevel</c>, so
/// <see cref="ComputeHotTickBase"/> uses <c>hot.Delta * level</c>. The old buff-path (A5) `level-1`
/// variant was a port bug (part of the port-wide Delta*(level-1) under-scaling) and has been unified.
/// A5 still applies no HealReceivedPct at its call site (unlike A2) — that divergence stays at the
/// call site, not in this calculator.
/// </summary>
public static class HealAmountCalculator
{
    public static float BoostMultiplier(Player effector)
    {
        float m = 1.0f + (effector.BonusHealBoost + effector.HealBoostDelta) / 1000f;
        if (effector.PassiveBonusHealSkillBoostPct > 0)
            m *= 1f + effector.PassiveBonusHealSkillBoostPct / 100f;
        if (effector.BonusHealSkillBoostPct > 0)
            m *= 1f + effector.BonusHealSkillBoostPct / 100f;
        return m;
    }

    public static int ComputeInstant(SkillHealInfo he, Creature target, int level, float boostMult)
    {
        int vd = he.BaseValue + he.Delta * level;
        int maxStat = he.HealType switch
        {
            "hp" => target.MaxHp,
            "mp" => target.MaxMp,
            "fp" => target is Player p ? p.EffectiveMaxFp : 0,
            "dp" => 6000,
            "vp" => 6000,
            _    => 0,
        };
        int heal = he.IsPercent ? maxStat * vd / 100 : vd;
        heal = (int)(heal * boostMult);
        if (target.HealReceivedPct != 0)
            heal = Math.Max(0, (int)(heal * (100 + target.HealReceivedPct) / 100f));
        return heal;
    }

    public static int ComputeHotTickBase(SkillHotInfo hot, int level)
        => Math.Max(1, hot.BaseValue + hot.Delta * level);
}
