using System.Linq;
using System.Collections.Generic;
using System;
// PashidAssaultPodAI2 — Java ai/instance/eternalBastion/PashidAssaultPodAI2.java. Periodically
// spawns a random trio of assault-pod adds around itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("pashid_assault_pod")]
public sealed class PashidAssaultPodAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        StartTimerPodSpawn();
        base.OnSpawned();
    }

    private void StartTimerPodSpawn()
    {
        ScheduleTask(() =>
        {
            // note: Java guarded each tick with isAlreadyDead()/cancelSpawnTask(); CancelTasks() already
            // stops this loop from the base OnDied() hook, so the guard is redundant here.
            SpawnAssault();
        }, 10000, 120000);
    }

    private void SpawnAssault()
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        int distance = Random.Shared.Next(0, 11);
        float x1 = MathF.Cos(MathF.PI * direction) * distance;
        float y1 = MathF.Sin(MathF.PI * direction) * distance;
        var p = Owner.Position;
        int rnd = Random.Shared.Next(1, 3);
        switch (rnd)
        {
            case 1:
                Spawn(231105, p.X + x1, p.Y + y1, p.Z, (byte)p.Heading);
                Spawn(231108, p.X + x1, p.Y + y1, p.Z, (byte)p.Heading);
                Spawn(231106, p.X + x1, p.Y + y1, p.Z, (byte)p.Heading);
                break;
            case 2:
                Spawn(231105, p.X + x1, p.Y + y1, p.Z, (byte)p.Heading);
                Spawn(231108, p.X + x1, p.Y + y1, p.Z, (byte)p.Heading);
                Spawn(231107, p.X + x1, p.Y + y1, p.Z, (byte)p.Heading);
                break;
        }
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the pod-spawn task (Java's cancelSpawnTask()).
    }
}
