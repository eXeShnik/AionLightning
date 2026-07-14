// SlowTimeAI2 — Java ai/instance/tiamatStrongHold/SlowTimeAI2.java. Zone hazard: debuffs players
// within 50m.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("slowedtime")]
public sealed class SlowTimeAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Player) return;
        if (Owner.Position.DistanceTo(creature.Position) <= 50 && !HasAbnormalEffect(creature, 20728))
        {
            UseSkill(20728);
        }
    }

    private static bool HasAbnormalEffect(Creature creature, int skillId) =>
        creature.GetActiveEffects().Any(e => e.SkillId == skillId);

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
