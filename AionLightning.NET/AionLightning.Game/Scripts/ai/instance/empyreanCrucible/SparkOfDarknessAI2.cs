// SparkOfDarknessAI2 — Java ai/instance/empyreanCrucible/SparkOfDarknessAI2.java. Short-lived hazard
// add: casts once shortly after spawn, then self-removes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("spark_of_darkness")]
public sealed class SparkOfDarknessAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (!getOwner().IsAlreadyDead)
                UseSkill(19554);
        }, 500);
        ScheduleTask(() =>
        {
            // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, 6500);
    }

    // note: Java also overrode ask (CAN_ATTACK_PLAYER = POSITIVE) — no C# equivalent hook exists.
}
