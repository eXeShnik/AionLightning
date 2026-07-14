// RanodimAI2 — Java ai/instance/sauroSupplyBase/RanodimAI2.java. Sauro Supply Base boss:
// hp-percentage skill stage while enraged.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ranodim")]
public sealed class RanodimAI2 : AggressiveNpcAI2
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
    }

    private void Stage1()
    {
        const int delay = 25000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(20702, 45);
        ScheduleDelayStage1(delay);
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
