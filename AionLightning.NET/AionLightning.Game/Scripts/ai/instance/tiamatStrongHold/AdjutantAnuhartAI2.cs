// AdjutantAnuhartAI2 — Java ai/instance/tiamatStrongHold/AdjutantAnuhartAI2.java. Tiamat Stronghold
// boss: chains a fixed skill rotation once engaged, and layers self-buffs at HP breakpoints.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("adjutantanuhart")]
public sealed class AdjutantAnuhartAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private readonly List<int> _percents = new();

    private void StartTask(int taskId)
    {
        switch (taskId)
        {
            case 1:
                // Schmerzwelle
                ScheduleTask(() => { UseSkill(20745); StartTask(2); }, 12000);
                break;
            case 2:
                // Ausladender Angriff
                ScheduleTask(() => { UseSkill(20746); StartTask(3); }, 10000);
                break;
            case 3:
                // Adjutanten-Schlag
                ScheduleTask(() => { UseSkill(20744); StartTask(4); }, 10000);
                break;
            case 4:
                // Wave of Pain
                ScheduleTask(() => { UseSkill(20745); StartTask(5); }, 3000);
                break;
            case 5:
                // Wirbelklinge
                ScheduleTask(() =>
                {
                    // Schild
                    Owner.Target = Owner;
                    UseSkill(20749);
                    // note: Java also fired skill 20747 directly via SkillEngine.getSkill(...).useNoAnimationSkill() —
                    // no scripted no-animation cast path exists yet, so only the shield (20749) is issued.
                    Spawn(283234, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
                    StartTask(6);
                }, 4000);
                break;
            case 6:
                // Wave of Pain, fallback to task1
                ScheduleTask(() => StartTask(1), 8000);
                break;
        }
    }

    public override void OnAttack(Creature attacker)
    {
        base.OnAttack(attacker);
        if (_isHome)
        {
            _isHome = false;
            // starte mit Wirbelklinge
            StartTask(5);
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 50:
                        ChooseBuff(20938);
                        break;
                    case 25:
                        ChooseBuff(20939);
                        break;
                    case 10:
                        ChooseBuff(20940);
                        CancelTasks();
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void ChooseBuff(int buff)
    {
        Owner.Target = Owner;
        UseSkill(buff);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 50, 25, 10 });
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
        CancelTasks();
        Owner.ClearAllEffects();
        _isHome = true;
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied();
        CancelTasks();
    }
}
