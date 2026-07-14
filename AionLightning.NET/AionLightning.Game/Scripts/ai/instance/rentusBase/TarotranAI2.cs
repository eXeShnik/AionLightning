// TarotranAI2 — Java ai/instance/rentusBase/TarotranAI2.java. Rentus Base boss: HP-breakpoint add-spawn
// event plus a rotating 3-stage self-buff cycle.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tarotran")]
public sealed class TarotranAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private bool _isStartedEvent;
    private int _buffNr = 1;

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isStartedEvent)
        {
            _isStartedEvent = true;
            Spawn(282386, 383.964f, 541.48f, 147.5f, 38);
            StartBuffCycle();
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                _percents.Remove(percent);
                UseSkill(19700);
                ScheduleTask(() =>
                {
                    int count = System.Random.Shared.Next(4, 9);
                    for (int i = 0; i < count; i++) RndSpawn(282385);
                    // note: Java then re-targeted the most-hated attacker (AggroList) to resume combat —
                    // aggro tracking stays owned by NpcAiService.
                }, 4000);
                break;
            }
        }
    }

    private void RndSpawn(int npcId)
    {
        double direction = Math.PI * (System.Random.Shared.Next(0, 200) / 100.0);
        int distance = System.Random.Shared.Next(1, 3);
        var p = Owner.Position;
        float x = p.X + (float)(Math.Cos(direction) * distance);
        float y = p.Y + (float)(Math.Sin(direction) * distance);
        Spawn(npcId, x, y, p.Z, (byte)p.Heading);
        // note: Java despawned this helper 15s later (NpcActions.delete) — scripted NPC delete isn't
        // exposed to scripts yet.
    }

    private void StartBuffCycle()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            int skill = _buffNr switch
            {
                1 => 19370,
                2 => 19371,
                _ => 19372,
            };
            _buffNr = _buffNr >= 3 ? 1 : _buffNr + 1;
            UseSkill(skill);
        }, 21000, 21000);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 85, 65, 55, 45, 30, 15 });
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTasks();
        _isStartedEvent = false;
        base.OnBackHome();
        Owner.RemoveEffectBySkillId(19370);
        Owner.RemoveEffectBySkillId(19371);
        Owner.RemoveEffectBySkillId(19372);
        // note: Java also deleted the summoned helper npcs (282386/282387/282530/282385) via
        // WorldMapInstance.getNpcs — NPC listing/delete isn't exposed to scripts yet.
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied();
        // note: Java also deleted the summoned helper npcs here (same set as OnBackHome).
    }
}
