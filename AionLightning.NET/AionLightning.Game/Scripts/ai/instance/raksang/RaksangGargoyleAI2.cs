// RaksangGargoyleAI2 — Java ai/instance/raksang/RaksangGargoyleAI2.java. Trap add: casts once shortly
// after spawn, then self-removes on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("raksang_gargoyle")]
public sealed class RaksangGargoyleAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (!getOwner().IsAlreadyDead)
                UseSkill(19126, 46);
        }, 2000);
    }

    // note: Java also overrode ask (CAN_RESIST_ABNORMAL = POSITIVE) — no C# equivalent hook exists.

    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }
}
