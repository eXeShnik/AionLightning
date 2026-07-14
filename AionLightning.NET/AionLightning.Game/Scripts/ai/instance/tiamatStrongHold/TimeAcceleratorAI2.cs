// TimeAcceleratorAI2 — Java ai/instance/tiamatStrongHold/TimeAcceleratorAI2.java. Zone hazard:
// debuffs creatures within 5m, then self-removes after 20s.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("timeaccelerator")]
public sealed class TimeAcceleratorAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (Owner.Position.DistanceTo(creature.Position) <= 5 && !HasAbnormalEffect(creature, 20727))
        {
            UseSkill(20727);
        }
    }

    private static bool HasAbnormalEffect(Creature creature, int skillId) =>
        creature.GetActiveEffects().Any(e => e.SkillId == skillId);

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java self-removed via getController().onDelete() 20s after spawn — no scripted
        // NPC-removal API exists yet.
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
