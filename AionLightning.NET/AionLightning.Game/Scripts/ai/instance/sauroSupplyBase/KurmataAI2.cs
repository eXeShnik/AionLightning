// KurmataAI2 — Java ai/instance/sauroSupplyBase/KurmataAI2.java. Sauro Supply Base boss:
// hp-percentage skill stages while enraged.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kurmata")]
public sealed class KurmataAI2 : AggressiveNpcAI2
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
        if (hpPercentage <= 50 && _stage < 1)
        {
            Stage1();
            _stage = 1;
        }
        if (hpPercentage <= 40 && _stage < 2)
        {
            Stage2();
            _stage = 2;
        }
    }

    private void Stage1()
    {
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(20701, 45);
    }

    private void Stage2()
    {
        const int delay = 20000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(20858, 45);
        ScheduleDelayStage2(delay);
    }

    private void ScheduleDelayStage2(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage2, delay);
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
