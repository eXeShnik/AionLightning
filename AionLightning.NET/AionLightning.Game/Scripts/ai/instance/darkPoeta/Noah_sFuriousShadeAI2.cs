// Noah_sFuriousShadeAI2 — Java ai/instance/darkPoeta/Noah_sFuriousShadeAI2.java. Spectral tree that
// casts a skill on spawn and chains a two-skill sequence once it drops to 30% HP.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("spectral_tree")]
public sealed class Noah_sFuriousShadeAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
        DoUseSkill();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents.ToArray())
        {
            if (hpPercentage <= percent)
            {
                if (percent == 30)
                {
                    UseSkill(18529);
                    UseSkillTree();
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void DoUseSkill()
    {
        if (Random.Shared.Next(2) > 0) UseSkill(16822, 50);
    }

    private void UseSkillTree()
    {
        ScheduleTask(() =>
        {
            UseSkill(17736, 50);
            ScheduleTask(() => UseSkill(18531, 50), 8000);
        }, 7000);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.Add(30);
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTasks();
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied(); // cancels the scheduled skill-tree tasks
    }
}
