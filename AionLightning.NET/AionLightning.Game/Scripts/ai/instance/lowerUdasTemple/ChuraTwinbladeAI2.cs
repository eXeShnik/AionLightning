// ChuraTwinbladeAI2Lower — Java ai/instance/lowerUdasTemple/ChuraTwinbladeAI2.java. Alternate/lower
// Uda's Temple variant of the Chura Twinblade boss. Renamed to ChuraTwinbladeAI2Lower to avoid a
// class-name collision with udasTempleLower/ChuraTwinbladeAI2.java: both Java classes live in distinct
// packages but this port's `Ai` namespace is intentionally flat (see AI_PORT_SPEC.md), and both carry
// the verbatim @AIName "churatwinblade" — AiEngine.Register uses a plain indexer so whichever class is
// scanned last silently wins that ai-name, mirroring the same last-registration-wins behavior Java's own
// AiName map would have for two classes sharing one name.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("churatwinblade")]
public sealed class ChuraTwinbladeAI2Lower : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java cast skill 18624 (Stigma Burst) via AI2Actions once HP dropped to 33% or below;
        // AI2Actions skill-casting isn't wired at the script layer yet.
    }

    public override AttackIntention ChooseAttackIntention()
        // note: Java picked SWITCH_TARGET/SKILL_ATTACK/FINISH_ATTACK via AggroList.getMostHated() and
        // SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are owned by
        // NpcAiService, not this script layer.
        => AttackIntention.SimpleAttack;
}
