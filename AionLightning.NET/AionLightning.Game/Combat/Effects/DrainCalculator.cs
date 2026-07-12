using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4c: pure, unit-testable extraction of CM_CASTSPELL's M239 drain proc-heal computation
/// (previously duplicated inline at 3 call sites: the AoE-target damage drain, the main
/// single-target damage drain, and the splash-target damage drain). BEHAVIOR-IDENTICAL relocation —
/// the formula below is copied verbatim from the original packet handler.
/// </summary>
public static class DrainCalculator
{
    public static (int HpGain, int MpGain) Compute(int damage, SkillDamageInfo fx, Player caster)
    {
        int hpGain = Math.Min(damage * fx.HpPercent / 100, caster.MaxHp - caster.CurrentHp);
        int mpGain = Math.Min(damage * fx.MpPercent / 100, caster.MaxMp - caster.CurrentMp);
        return (hpGain, mpGain);
    }
}
