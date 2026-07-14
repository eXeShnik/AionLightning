using System.Linq;
using System.Collections.Generic;
using System;
// TualisDrainedMinionAI2 — Java ai/instance/elementisForest/TualisDrainedMinionAI2.java. Temporary
// add that self-expires 30s after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("tualis_drained_minion")]
public sealed class TualisDrainedMinionAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        StartLifeTask();
    }

    private void StartLifeTask()
    {
        ScheduleTask(() =>
        {
            // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, 30000);
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }
}
