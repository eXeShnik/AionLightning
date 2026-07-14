// GravityAI2 — Java ai/instance/tiamatStrongHold/GravityAI2.java. Zone hazard: debuffs players
// within 50m and a small vertical band.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("gravity")]
public sealed class GravityAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Player) return;
        if (Owner.Position.DistanceTo(creature.Position) <= 50
            && creature.Position.Z - Owner.Position.Z <= 4
            && !HasAbnormalEffect(creature, 20738))
        {
            UseSkill(20738);
        }
    }

    private static bool HasAbnormalEffect(Creature creature, int skillId) =>
        creature.GetActiveEffects().Any(e => e.SkillId == skillId);

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
