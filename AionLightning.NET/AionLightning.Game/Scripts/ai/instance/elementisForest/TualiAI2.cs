using System.Linq;
using System.Collections.Generic;
using System;
// TualiAI2 — Java ai/instance/elementisForest/TualiAI2.java. Boss: on first attack starts a skill
// rotation and a periodic add-spawn task, then layers self-buffs at 65/45/25% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tuali")]
public sealed class TualiAI2 : AggressiveNpcAI2
{
    private bool _isStart;
    private bool _isStart65Event;
    private bool _isStart45Event;
    private bool _isStart25Event;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isStart)
        {
            _isStart = true;
            SendMsg(1500454);
            ScheduleSkills();

            ScheduleTask(() =>
            {
                if (!Owner.IsAlreadyDead)
                {
                    UseSkill(19348);
                    // note: Java capped total 282308 adds at 12 by counting WorldMapInstance.getNpcs
                    // (282308).size() before each spawn; bulk npc-id lookup isn't exposed to scripts yet,
                    // so this always spawns the full batch below.
                    for (int i = 0; i < 6; i++)
                        RndSpawn(282307);
                    SendMsg(1401378);
                }
            }, 20000, 50000);
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 65 && !_isStart65Event)
        {
            _isStart65Event = true;
            Buff();
        }
        if (hpPercentage <= 45 && !_isStart45Event)
        {
            _isStart45Event = true;
            Buff();
        }
        if (hpPercentage <= 25 && !_isStart25Event)
        {
            _isStart25Event = true;
            Buff();
        }
    }

    private void Buff()
    {
        UseSkill(19511);
        SendMsg(1500456);
        SendMsg(1401041);
    }

    private void RndSpawn(int npcId)
    {
        double direction = Math.PI * (Random.Shared.Next(0, 200) / 100.0);
        int distance = Random.Shared.Next(5, 13);
        float x1 = (float)(Math.Cos(direction) * distance);
        float y1 = (float)(Math.Sin(direction) * distance);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }

    public override void OnDied()
    {
        CancelTasks();
        base.OnDied();
        SendMsg(1500457);
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        CancelTasks();
        DeleteNpcs(282307);
        DeleteNpcs(282308);
        base.OnBackHome();
        _isStart65Event = false;
        _isStart45Event = false;
        _isStart25Event = false;
        _isStart = false;
    }

    private void DeleteNpcs(int npcId)
    {
        // note: Java deleted every live npc with this id via WorldMapInstance.getNpcs(id) +
        // getController().onDelete(); bulk npc-id lookup and scripted despawn aren't exposed to scripts
        // yet.
    }

    private void ScheduleSkills()
    {
        if (Owner.IsAlreadyDead || !_isStart)
            return;
        ScheduleTask(() =>
        {
            UseSkill(19512 + Random.Shared.Next(5));
            ScheduleSkills();
        }, Random.Shared.Next(18, 23) * 1000);
    }
}
