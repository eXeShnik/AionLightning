// QueenAlukinaAI2 — Java ai/instance/empyreanCrucible/QueenAlukinaAI2.java. Empyrean Crucible boss:
// HP-threshold skill escalation (75%/50%/25%), the last stage looping a repeating skill barrage.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("alukina_emp")]
public sealed class QueenAlukinaAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercents();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        _percents.Clear();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        base.OnDied();
    }

    public override void OnBackHome()
    {
        AddPercents();
        CancelTasks();
        base.OnBackHome();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    private void StartEvent(int percent)
    {
        UseSkill(17899, 41);

        switch (percent)
        {
            case 75:
                ScheduleSkill(17900, 4500);
                // note: Java shouted 340487 via NpcShoutsService here; NPC shouts aren't exposed to
                // scripts yet.
                ScheduleSkill(17899, 14000);
                ScheduleSkill(17900, 18000);
                break;
            case 50:
                ScheduleSkill(17280, 4500);
                ScheduleSkill(17902, 8000);
                break;
            case 25:
                ScheduleTask(() =>
                {
                    if (getOwner().IsAlreadyDead)
                    {
                        CancelTasks();
                        return;
                    }
                    UseSkill(17901, 41);
                    ScheduleSkill(17902, 5500);
                    ScheduleSkill(17902, 7500);
                }, 4500, 20000);
                break;
        }
    }

    private void ScheduleSkill(int skillId, int delay)
    {
        ScheduleTask(() =>
        {
            if (!getOwner().IsAlreadyDead)
                UseSkill(skillId, 41);
        }, delay);
    }

    private void CheckPercentage(int percentage)
    {
        foreach (var percent in _percents)
        {
            if (percentage > percent) continue;
            _percents.Remove(percent);
            StartEvent(percent);
            break;
        }
    }

    private void AddPercents()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }
}
