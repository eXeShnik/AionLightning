// CaptainRohukaAI2 — Java ai/instance/sauroSupplyBase/CaptainRohukaAI2.java. Sauro Supply Base boss:
// hp-percentage skill stages while enraged.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rohuka")]
public sealed class CaptainRohukaAI2 : AggressiveNpcAI2
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
        if (hpPercentage <= 50 && _stage < 1)
        {
            Stage1();
            _stage = 1;
        }
        if (hpPercentage <= 45 && _stage < 2)
        {
            Stage2();
            _stage = 2;
        }
        if (hpPercentage <= 25 && _stage < 3)
        {
            Stage3();
            _stage = 3;
        }
    }

    private void Stage1()
    {
        if (Owner.IsAlreadyDead || !_isStart) return;
        UseSkill(21135, 50);
    }

    private void Stage2()
    {
        const int delay = 35000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        Skill();
        ScheduleDelayStage2(delay);
    }

    private void Skill()
    {
        UseSkill(18158, 100);
        ScheduleTask(() => UseSkill(18160, 100), 4000);
    }

    private void ScheduleDelayStage2(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(Stage2, delay);
    }

    private void Stage3()
    {
        const int delay = 15000;
        if (Owner.IsAlreadyDead || !_isStart) return;
        ScheduleDelayStage3(delay);
    }

    private void ScheduleDelayStage3(int delay)
    {
        if (!_isStart && !Owner.IsAlreadyDead) return;
        ScheduleTask(() =>
        {
            PickRandomTarget();
            Stage3();
        }, delay);
    }

    /// <summary>Java <c>getRandomTarget</c> — kept under a distinct name here since it overrode
    /// <see cref="AggressiveNpcAI2"/>'s <c>GetRandomTarget</c> helper, which isn't virtual: picked a
    /// random living player within 16m from the known-list and restarted the aggro list on it.</summary>
    private void PickRandomTarget()
    {
        // note: known-list/aggro-list membership isn't exposed to scripts yet.
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
