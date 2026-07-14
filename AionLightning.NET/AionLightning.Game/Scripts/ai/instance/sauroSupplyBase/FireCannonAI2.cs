// FireCannonAI2 — Java ai/instance/sauroSupplyBase/FireCannonAI2.java. Sauro Supply Base fire
// cannon: fires a no-animation skill at its target once, then stops (note: Java's guard condition
// looks inverted — it only fires when the owner is already dead — preserved verbatim).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("fire_cannon")]
public sealed class FireCannonAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        StartSkillTask();
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) UseSkill(21201, 65);
            CancelTasks();
        }, 1, 1000);
    }

    // note: Java also overrode modifyDamage to always return 1; no damage-modification hook exists on
    // NpcAi2 yet.
}
