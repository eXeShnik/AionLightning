// ShebaAI2 — Java ai/instance/sauroSupplyBase/ShebaAI2.java. Sauro Supply Base boss: hp-percentage
// skill stages that spawn/despawn adds while enraged.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("sheba")]
public sealed class ShebaAI2 : AggressiveNpcAI2
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
        CheckPercentage(Owner.HpPercentage);
        WakeUp();
    }

    private void WakeUp() => _isStart = true;

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 75 && _stage < 1)
        {
            Stage1();
            _stage = 1;
        }
        if (hpPercentage <= 70 && _stage < 2)
        {
            Stage2();
            _stage = 2;
        }
        if (hpPercentage <= 50 && _stage < 3)
        {
            Stage3();
            _stage = 3;
        }
        if (hpPercentage <= 25 && _stage < 4)
        {
            Stage4();
            _stage = 4;
        }
    }

    private void Stage1()
    {
        const int delay = 25000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        SendMsg(1500775);
        UseSkill(21188, 25);
        ScheduleDelayStage1(delay);
    }

    private void Stage2()
    {
        if (Owner.IsAlreadyDead || !_isStart) return;
        SendMsg(1500774);
        UseSkill(21189, 0);
        Spawn(284435, 900.12497f, 879.17401f, 411.625f);
        Spawn(284435, 887.1312f, 889.20688f, 411.875f);
        Spawn(284435, 900.1312f, 901.20688f, 411.875f);
    }

    private void Stage3()
    {
        const int delay = 40000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        SendMsg(1500777);
        UseSkill(21183, 25);
        ScheduleDelayStage3(delay);
    }

    private void Stage4()
    {
        const int delay = 45000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        SendMsg(1500776);
        UseSkill(21184, 25);
        switch (Random.Shared.Next(1, 3))
        {
            case 1:
                DespawnNpcs(284436);
                Spawn(284436, 900.12497f, 889.17401f, 412.1f);
                break;
            case 2:
                DespawnNpcs(284436);
                Spawn(284436, 913.12497f, 876.17401f, 412.1f, (byte)45);
                Spawn(284436, 900.12497f, 870.17401f, 412.1f, (byte)30);
                Spawn(284436, 886.12497f, 876.17401f, 412.1f, (byte)16);
                Spawn(284436, 881.12497f, 889.17401f, 412.1f);
                Spawn(284436, 899.12497f, 909.17401f, 412.1f, (byte)90);
                Spawn(284436, 913.12497f, 902.17401f, 412.1f, (byte)78);
                Spawn(284436, 918.12497f, 890.17401f, 412.1f, (byte)61);
                break;
        }
        ScheduleDelayStage4(delay);
    }

    private void ScheduleDelayStage4(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage4, delay);
    }

    private void ScheduleDelayStage3(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage3, delay);
    }

    private void ScheduleDelayStage1(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage1, delay);
    }

    /// <summary>Java iterated every <paramref name="npcId"/> instance in the owner's world/instance and
    /// deleted them; bulk lookup-by-npcId and NPC deletion aren't exposed to scripts yet (<see cref="NpcAi2.GetNpc"/>
    /// only resolves the first match).</summary>
    private void DespawnNpcs(int npcId)
    {
        // note: not wired — see summary above.
    }

    public override void OnBackHome()
    {
        DespawnNpcs(284435);
        DespawnNpcs(284436);
        base.OnBackHome();
        _isStart = false;
        _stage = 0;
    }

    public override void OnDied()
    {
        DespawnNpcs(284435);
        DespawnNpcs(284436);
        base.OnDied();
        _isStart = false;
        _stage = 0;
    }
}
