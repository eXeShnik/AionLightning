// VengeFulOrbAI2 — Java ai/instance/danuarReliquary/VengeFulOrbAI2.java. Timed hazard orb: pulses
// a self-cast skill every 2s then self-deletes shortly after spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("vengefulorb")]
public sealed class VengeFulOrbAI2 : NpcAi2
{
    private const int OrbNpcId = 284443;

    public override void OnSpawned()
    {
        base.OnSpawned();
        int skill = Owner.Template.NpcId == OrbNpcId ? 21178 : 0;
        if (skill == 0) return;

        ScheduleTask(() => UseSkill(skill), 0, 2000);
        ScheduleTask(() =>
        {
            // note: Java despawned the owner via AI2Actions.deleteOwner here; no scripted despawn API
            // exists yet.
        }, 1000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
