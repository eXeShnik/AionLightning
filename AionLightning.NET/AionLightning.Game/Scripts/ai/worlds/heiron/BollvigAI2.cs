// BollvigAI2 — Java ai/worlds/heiron/BollvigAI2.java. Open-world boss: HP-percentage-gated skill/
// summon phases (Sleep of Death, Nerve Absorption, Charming Attraction, Curse of Soul...).
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("bollvig")]
public sealed class BollvigAI2 : AggressiveFirstSkillAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        AddPercent();
        base.OnSpawned();
        // note: Java also deleted a stray npc 204655 in the same instance via WorldMapInstance.getNpc;
        // multi-npc instance lookups aren't exposed to scripts (GetNpc only returns the first match in
        // the owner's own scope).
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (int percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 75:
                case 50:
                    CancelTask();
                    UseFirstSkillTree();
                    break;
                case 25:
                    CancelTask();
                    FirstSkill();
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void UseFirstSkillTree()
    {
        UseSkill(17861); // Sleep of Death
        RndSpawnInRange(280802);
        RndSpawnInRange(280802);
        RndSpawnInRange(280803);
        RndSpawnInRange(280803);
        FirstSkill();
    }

    private void FirstSkill()
    {
        int hpPercent = Owner.HpPercentage;
        if (hpPercent <= 50 && hpPercent > 25)
        {
            ScheduleTask(() =>
            {
                UseSkill(18034); // Nerve Absorption
                RndSpawnInRange(280804);
            }, 10000);
        }
        else if (hpPercent <= 25)
        {
            UseSkill(18037); // Blood Cell Destruction
        }
        ScheduleTask(SkillThree, 31000);
    }

    private void SkillThree()
    {
        UseSkill(17899); // Charming Attraction
        ScheduleTask(() =>
        {
            int hpPercent = Owner.HpPercentage;
            if (hpPercent <= 75 && hpPercent > 50)
            {
                UseSkill(18025); // Curse of Soul
                FirstSkill();
            }
            else if (hpPercent <= 50)
            {
                UseSkill(18025); // Curse of Soul
                FirstSkill();
            }
            else if (hpPercent <= 25)
            {
                UseSkill(18027); // Mortal Cutting
                ScheduleTask(SkillThree, 11000);
            }
        }, 5000);
    }

    /// <summary>Java tracked 4 separate Future handles (first/second/third/lastTask) and cancelled
    /// whichever was still pending; NpcAi2.CancelTasks() cancels every pending ScheduleTask instead.</summary>
    private void CancelTask() => CancelTasks();

    private void RndSpawnInRange(int npcId)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        float x = MathF.Cos(MathF.PI * direction) * 10;
        float y = MathF.Sin(MathF.PI * direction) * 10;
        Spawn(npcId, 1001 + x, 2828 + y, 235.66f);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTask();
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        CancelTask();
        // note: Java deleted its spawned summons (280802/280803/280804) here via WorldMapInstance.getNpcs;
        // multi-npc instance lookups aren't exposed to scripts.
        base.OnDespawned();
        // note: Java also re-spawned npc 204655 here when checkNpc() (both 204655 and 212314 gone/dead)
        // held true; instance-wide npc-existence checks aren't exposed to scripts.
    }

    public override void OnDied()
    {
        _percents.Clear();
        CancelTask();
        // note: same spawned-summon cleanup gap as OnDespawned.
        base.OnDied();
        // note: same 204655-respawn gap as OnDespawned.
    }
}
