// VirhanaTheGreatAI2 — Java ai/instance/beshmundirTemple/VirhanaTheGreatAI2.java. Beshmundir Temple
// boss: shouts at 50% HP and runs a repeating rage escalation (up to 12 ticks before resetting).
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("virhana")]
public sealed class VirhanaTheGreatAI2 : AggressiveNpcAI2
{
    private const int RageStartSkillId = 19121;
    private const int RageTickSkillId = 18897;
    private const int MaxRageTicks = 12;
    private const int RageIntervalMs = 70000;
    private const int RageTickIntervalMs = 10000;

    private bool _isHome = true;
    private int _rageCount;
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500064);
            ScheduleRage();
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the rage tasks
        _percents.Clear();
    }

    public override void OnBackHome()
    {
        CancelTasks();
        base.OnBackHome();
        _isHome = true;
        AddPercent();
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.Add(50);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            SendMsg(1500065);
            _percents.Remove(percent);
            break;
        }
    }

    private void ScheduleRage()
    {
        if (Owner.IsAlreadyDead || _isHome) return;
        UseSkill(RageStartSkillId);
        ScheduleTask(StartRage, RageIntervalMs);
    }

    private void StartRage()
    {
        if (Owner.IsAlreadyDead || _isHome) return;
        if (_rageCount < MaxRageTicks)
        {
            SendMsg(1500066);
            UseSkill(RageTickSkillId);
            _rageCount++;
            ScheduleTask(StartRage, RageTickIntervalMs);
        }
        else
        {
            _rageCount = 0;
            ScheduleRage();
        }
    }
}
