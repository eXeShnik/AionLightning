// GoldenTatarAI2 — Java ai/worlds/tiamaranta/GoldenTatarAI2.java. Open-world "Golden Tatar" boss:
// HP-percentage-gated stun/rage phases that cast skills and spawn helper adds.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("golden_tatar")]
public sealed class GoldenTatarAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private bool _isAggred;
    private int _curentPercent = 100;

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isAggred)
        {
            _isAggred = true;
            StartSpecialSkillTask();
            SendMsg(1500499);
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        _curentPercent = hpPercentage;
        foreach (int percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 90:
                case 70:
                case 44:
                case 23:
                    CancelTasks();
                    // note: Java also toggled canThink()=false and called EmoteManager.emoteStopAttacking;
                    // neither hook is exposed to scripts yet.
                    UseSkill(20483, 60);
                    SendMsg(1500501);
                    ScheduleTask(() =>
                    {
                        if (Owner.IsAlreadyDead) return;
                        UseSkill(20216, 60);
                        StartThinkTask();
                        for (int i = 0; i < 6; i++) RndSpawn(282746);
                    }, 3500);
                    break;
                default:
                    StartPhaseTask();
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void StartThinkTask()
    {
        ScheduleTask(() =>
        {
            // note: Java re-evaluated its most-hated target via getAggroList()/getMoveController() here
            // and toggled canThink() back on; aggro-list/move-controller access isn't exposed to scripts.
        }, 20000);
    }

    private void StartPhaseTask()
    {
        UseSkill(20481, 60);
        SendMsg(1500500);
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            // note: Java deleted every existing npc 282743 in scope first; multi-npc scope deletion isn't
            // exposed to scripts yet (GetNpc only returns the first match).
            for (int i = 0; i < 8; i++) RndSpawn(282743);
            StartSpecialSkillTask();
        }, 4000);
    }

    private void StartSpecialSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            UseSkill(20223, 60);
            ScheduleTask(() =>
            {
                if (Owner.IsAlreadyDead) return;
                UseSkill(20224, 60);
                ScheduleTask(() =>
                {
                    if (Owner.IsAlreadyDead) return;
                    UseSkill(20224, 60);
                    if (_curentPercent <= 63)
                    {
                        ScheduleTask(() =>
                        {
                            if (Owner.IsAlreadyDead) return;
                            UseSkill(20480, 60);
                            SendMsg(1500502);
                            ScheduleTask(() =>
                            {
                                if (Owner.IsAlreadyDead) return;
                                // note: Java deleted existing npc 282744 in scope first (see StartPhaseTask
                                // note).
                                RndSpawn(282744);
                                RndSpawn(282744);
                            }, 2000);
                        }, 21000);
                    }
                }, 3500);
            }, 1500);
        }, 12000);
    }

    private void RndSpawn(int npcId)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        int distance = Random.Shared.Next(1, 26);
        float x1 = MathF.Cos(MathF.PI * direction) * distance;
        float y1 = MathF.Sin(MathF.PI * direction) * distance;
        Spawn(npcId, 538.0332f + x1, 2789.2104f + y1, 78.95826f, (byte)Owner.Position.Heading);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 90, 84, 79, 75, 72, 70, 67, 63, 59, 53, 47, 44, 43, 39, 35, 30, 26, 23, 21, 16, 11, 6 });
    }

    public override void OnDespawned()
    {
        CancelTasks();
        _percents.Clear();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        SendMsg(1500503);
        CancelTasks();
        _percents.Clear();
        // note: Java deleted every existing npc 282746/282743/282744 in scope here; multi-npc scope
        // deletion isn't exposed to scripts yet.
        base.OnDied();
    }

    public override void OnBackHome()
    {
        CancelTasks();
        AddPercent();
        _curentPercent = 100;
        // note: Java deleted every existing npc 282746/282743/282744 in scope here (see OnDied note).
        _isAggred = false;
        base.OnBackHome();
    }
}
