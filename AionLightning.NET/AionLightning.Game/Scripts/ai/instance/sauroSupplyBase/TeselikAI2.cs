// TeselikAI2 — Java ai/instance/sauroSupplyBase/TeselikAI2.java. Sauro Supply Base boss:
// hp-percentage skill stage that spawns adds while enraged.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("teselik")]
public sealed class TeselikAI2 : AggressiveNpcAI2
{
    private int _stage;
    private bool _isStart;

    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        WakeUp();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        WakeUp();
        CheckPercentage(Owner.HpPercentage);
    }

    private void WakeUp() => _isStart = true;

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 90 && _stage < 1)
        {
            Stage1();
            _stage = 1;
        }
    }

    private void Stage1()
    {
        const int delay = 50000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(20657, 45);
        switch (Random.Shared.Next(1, 3))
        {
            case 1: SpawnGroupA(); break;
            case 2: SpawnGroupB(); break;
        }
        ScheduleDelayStage1(delay);
    }

    /// <summary>Java <c>random()</c>: spawns 2 adds at fixed points 3s later.</summary>
    private void SpawnGroupA()
    {
        if (Owner.IsAlreadyDead) return;
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                Spawn(284455, 472.12497f, 344.17401f, 181.625f);
                Spawn(284455, 485.1312f, 344.20688f, 181.875f);
            }
        }, 3000);
    }

    /// <summary>Java <c>random2()</c>: spawns 2 adds at a different pair of fixed points 3s later.</summary>
    private void SpawnGroupB()
    {
        if (Owner.IsAlreadyDead) return;
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                Spawn(284455, 472.12497f, 328.17401f, 181.625f);
                Spawn(284455, 487.1312f, 327.20688f, 181.875f);
            }
        }, 3000);
    }

    private void ScheduleDelayStage1(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage1, delay);
    }

    /// <summary>Java iterated every <paramref name="npcId"/> instance in the owner's world/instance and
    /// deleted them; bulk lookup-by-npcId and NPC deletion aren't exposed to scripts yet.</summary>
    private void DespawnNpcs(int npcId)
    {
        // note: not wired — see summary above.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        DespawnNpcs(284455);
        _isStart = false;
        _stage = 0;
    }

    public override void OnDied()
    {
        base.OnDied();
        DespawnNpcs(284455);
        _isStart = false;
        _stage = 0;
    }
}
