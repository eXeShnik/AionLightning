using System.Linq;
using System.Collections.Generic;
using System;
// CraterAI2 — Java ai/instance/pvpArenas/CraterAI2.java. PvP-arena hazard: periodically casts a
// crater skill on itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("crater")]
public sealed class CraterAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        StartEventTask();
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    private void StartEventTask()
    {
        ScheduleTask(() =>
        {
            // note: Java guarded each tick with isAlreadyDead(); CancelTasks() already stops this loop
            // from the base OnDied() hook, so the guard is redundant here.
            UseSkill(20057);
        }, 1000, 3000);
    }
}
