// DerakanakAI2 — Java ai/instance/sauroSupplyBase/DerakanakAI2.java. Sauro Supply Base boss:
// hp-percentage skill stages (note: the original only ever re-invokes Stage1 — Stage2 is dead code
// reachable only by direct call, preserved verbatim).
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("derakanak")]
public sealed class DerakanakAI2 : AggressiveNpcAI2
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
        if (hpPercentage <= 75 && _stage < 1)
        {
            Stage1();
            _stage = 1;
        }
        if (hpPercentage <= 20 && _stage < 2)
        {
            Stage1();
            _stage = 2;
        }
    }

    private void Stage1()
    {
        const int delay = 45000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(17888, 45);
        ScheduleDelayStage1(delay);
    }

    /// <summary>Java cast this on <c>getTarget()</c>; UseSkill's stub has no target parameter, so the
    /// target is dropped. Never reached from <see cref="CheckPercentage"/> in the original (see file
    /// summary), kept for structural parity.</summary>
    private void Stage2()
    {
        const int delay = 15000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(Random.Shared.Next(2) == 0 ? 16918 : 16881, 45);
        ScheduleDelayStage2(delay);
    }

    private void ScheduleDelayStage2(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage2, delay);
    }

    private void ScheduleDelayStage1(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage1, delay);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isStart = false;
        _stage = 0;
    }

    public override void OnDied()
    {
        base.OnDied();
        _isStart = false;
        _stage = 0;
    }
}
