// GuardDanuarAI2 — Java ai/instance/sauroSupplyBase/GuardDanuarAI2.java. Sauro Supply Base guard:
// self-casts a power skill on a repeating timer.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("danuar")]
public sealed class GuardDanuarAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java also self-targeted via AI2Actions.targetSelf before each cast; target-self isn't
        // exposed to scripts yet.
        ScheduleTask(() => UseSkill(21185), 3000, 5000);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the repeating skill task
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
