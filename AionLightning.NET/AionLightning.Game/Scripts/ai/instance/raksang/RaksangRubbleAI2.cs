// RaksangRubbleAI2 — Java ai/instance/raksang/RaksangRubbleAI2.java. Short-lived hazard add: casts
// once shortly after spawn, then self-removes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("raksang_rubble")]
public sealed class RaksangRubbleAI2 : AggressiveNpcAI2
{
    // note: Java also overrode canThink to always return false — no C# equivalent hook exists.

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            UseSkill(19937, 46);
            StartLifeTask();
        }, 1000);
    }

    private void StartLifeTask()
    {
        ScheduleTask(() =>
        {
            // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, 9000);
    }

    // note: Java also overrode ask (CAN_RESIST_ABNORMAL = POSITIVE) — no C# equivalent hook exists.

    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }
}
