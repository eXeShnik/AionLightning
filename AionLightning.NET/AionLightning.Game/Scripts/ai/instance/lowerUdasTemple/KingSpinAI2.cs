// KingSpinAI2 — Java ai/instance/lowerUdasTemple/KingSpinAI2.java. King Spin: casts a skill once HP
// drops to 50% or below.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kingspin")]
public sealed class KingSpinAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java cast skill 18609 (Exsanguinate) via AI2Actions once HP dropped to 50% or below;
        // AI2Actions skill-casting isn't wired at the script layer yet.
    }

    public override AttackIntention ChooseAttackIntention()
        // note: Java picked SWITCH_TARGET/SKILL_ATTACK/FINISH_ATTACK via AggroList.getMostHated() and
        // SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are owned by
        // NpcAiService, not this script layer.
        => AttackIntention.SimpleAttack;
}
