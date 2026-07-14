// EarthQuakeAI2 — Java ai/instance/tiamatStrongHold/EarthQuakeAI2.java. Zone hazard: debuffs
// players within 5m, then self-removes after 9s.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("earthquake")]
public sealed class EarthQuakeAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Player) return;
        if (Owner.Position.DistanceTo(creature.Position) <= 5 && !HasAbnormalEffect(creature, 20718))
        {
            UseSkill(20718);
        }
    }

    private static bool HasAbnormalEffect(Creature creature, int skillId) =>
        creature.GetActiveEffects().Any(e => e.SkillId == skillId);

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java self-removed via getController().onDelete() 9s after spawn — no scripted
        // NPC-removal API exists yet.
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
