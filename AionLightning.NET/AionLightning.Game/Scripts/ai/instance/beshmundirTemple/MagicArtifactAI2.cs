// MagicArtifactAI2 — Java ai/instance/beshmundirTemple/MagicArtifactAI2.java. Attack-triggered skill
// cast with a 1-second cooldown to prevent skill-use overflow.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("magicartifact")]
public sealed class MagicArtifactAI2 : AggressiveNpcAI2
{
    private const int SkillId = 18916;
    private const int CooldownMs = 1000;

    private bool _cooldown;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_cooldown) return;
        UseSkill(SkillId);
        _cooldown = true;
        ScheduleTask(() => _cooldown = false, CooldownMs);
    }
}
