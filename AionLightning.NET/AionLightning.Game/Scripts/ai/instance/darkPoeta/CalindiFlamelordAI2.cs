// CalindiFlamelordAI2 — Java ai/instance/darkPoeta/CalindiFlamelordAI2.java. Boss that spawns
// helper npcs at two HP thresholds and self-destructs on a 10-minute enrage timer once engaged.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("calindiflamelord")]
public sealed class CalindiFlamelordAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private bool _isStart;

    public override void OnSpawned()
    {
        AddPercent();
        base.OnSpawned();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
        if (!_isStart)
        {
            _isStart = true;
            CheckTimer();
        }
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents.ToArray())
        {
            if (hpPercentage <= percent)
            {
                // note: Java also called EmoteManager.emoteStopAttacking(getOwner()) at both branches;
                // not wired at the script layer yet.
                UseSkill(18233, 50);
                if (percent == 60)
                {
                    ScheduleTask(() => Sp(281267), 3000);
                }
                else
                {
                    ScheduleTask(() =>
                    {
                        Sp(281268);
                        Sp(281268);
                    }, 3000);
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void Sp(int npcId)
    {
        if (npcId == 281267)
        {
            Spawn(npcId, 1191.2714f, 1220.5795f, 144.2901f, 36);
            Spawn(npcId, 1188.3695f, 1257.1322f, 139.66028f, 80);
            Spawn(npcId, 1177.1423f, 1253.9136f, 140.58705f, 97);
            Spawn(npcId, 1163.5889f, 1231.9149f, 145.40042f, 118);
        }
        else
        {
            float direction = Random.Shared.Next(0, 200) / 100f;
            int distance = Random.Shared.Next(0, 3);
            float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
            float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
            Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z, (byte)Owner.Position.Heading);
        }
    }

    private void CheckTimer()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            // note: Java called EmoteManager.emoteStopAttacking(getOwner()) here; not wired at the
            // script layer yet.
            SendMsg(1400259);
            UseSkill(19679, 50);
            ScheduleTask(() =>
            {
                if (!Owner.IsAlreadyDead)
                {
                    // note: Java self-deleted via getController().onDelete(); self-delete isn't wired
                    // at the script layer yet.
                    SendMsg(1400260);
                }
            }, 2000);
        }, 600000);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 60, 30 });
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied();
    }
}
