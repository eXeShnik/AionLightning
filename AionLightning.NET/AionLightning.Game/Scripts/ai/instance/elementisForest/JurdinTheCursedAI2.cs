using System.Linq;
using System.Collections.Generic;
using System;
// JurdinTheCursedAI2 — Java ai/instance/elementisForest/JurdinTheCursedAI2.java. Boss: once below
// 90% HP, periodically spawns decorative flowers plus a wave of shadow adds around itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("jurdin")]
public sealed class JurdinTheCursedAI2 : SummonerAI2
{
    private bool _isStart;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java's per-percentage summon hook (handleIndividualSpawnedSummons, spawning 282190-
        // 282197 helper waves at 80/60/40/20% HP) has no equivalent on SummonerAI2 here — the loaded
        // percentage/summon-group templates it relied on aren't ported (see SummonerAI2.OnAttack note).
        if (Owner.HpPercentage <= 90 && !_isStart)
        {
            _isStart = true;
            StartTask();
        }
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isStart = false;
        CancelTasks();
    }

    public override void OnDied()
    {
        base.OnDied();
        CancelTasks();
    }

    private void StartTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead)
                CancelTasks();
            else
                SpawnShadows();
        }, 0, 60000);
    }

    private void SpawnShadows()
    {
        SpawnFlowers();
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead || !_isStart)
                return;
            for (int i = 0; i < 7; i++)
                RndSpawnInRange(282201, 10);
        }, 5000);
    }

    private void RndSpawnInRange(int npcId, int dist)
    {
        double direction = Math.PI * (Random.Shared.Next(0, 200) / 100.0);
        float x1 = (float)(Math.Cos(direction) * dist);
        float y1 = (float)(Math.Sin(direction) * dist);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
        // note: Java tracked this spawn's objectId via addHelpersSpawn for SummonerAI2's despawn-on-
        // backhome cleanup; helper-spawn tracking isn't ported yet (see SummonerAI2.OnDespawned/OnBackHome).
    }

    private void SpawnFlowers()
    {
        Spawn(282440, 460.795f, 801.471f, 130.759f);
        Spawn(282440, 485.118f, 790.683f, 129.668f);
        Spawn(282440, 474.227f, 777.928f, 128.875f);
        Spawn(282440, 475.009f, 783.226f, 128.875f);
        Spawn(282440, 459.194f, 797.047f, 130.491f);
        Spawn(282440, 484.584f, 791.999f, 129.750f);
        Spawn(282440, 485.502f, 803.050f, 130.472f);
        Spawn(282440, 486.006f, 804.645f, 130.540f);
        Spawn(282440, 412.354f, 800.439f, 131.176f);
        Spawn(282440, 474.676f, 816.744f, 131.281f);
        Spawn(282440, 412.354f, 800.439f, 131.176f);
        Spawn(282440, 467.355f, 811.444f, 131.261f);
        Spawn(282440, 463.336f, 798.282f, 130.309f);
    }
}
