// BrigadeGeneralTahabataAI2 — Java ai/instance/tiamatStrongHold/BrigadeGeneralTahabataAI2.java.
// Tiamat Stronghold boss: HP-breakpoint lava/adds events plus periodic piercing-strike and
// fire-storm phases.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("brigadegeneraltahabata")]
public sealed class BrigadeGeneralTahabataAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private bool _isEndPiercingStrike = true;
    private bool _isEndFireStorm = true;
    private readonly List<int> _percents = new();

    public override void OnAttack(Creature attacker)
    {
        base.OnAttack(attacker);
        if (_isHome)
        {
            _isHome = false;
            // note: Java closed instance door 610 (getWorldMapInstance().getDoors()) on engage —
            // instance doors aren't exposed to scripts yet.
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void StartPiercingStrikeTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) CancelTasks();
            else StartPiercingStrikeEvent();
        }, 15000, 20000);
    }

    private void StartFireStormTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) CancelTasks();
            else StartFireStormEvent();
        }, 10000, 20000);
    }

    private void StartFireStormEvent()
    {
        if (GetNpc(283045) is null)
        {
            UseSkill(20758);
            RndSpawn(283045, System.Random.Shared.Next(1, 5));
        }
    }

    private void StartPiercingStrikeEvent()
    {
        // note: Java teleported a random known player within 40m to the boss before casting skill
        // 20754 (SkillEngine.useNoAnimationSkill) and, only if it actually hit someone, followed up
        // with skill 20755. Known-player enumeration, forced teleport, and hit-detection aren't
        // exposed to scripts yet — the follow-up skill fires unconditionally as a best effort.
        ScheduleTask(() => UseSkill(20755), 3000);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 96:
                        LavaEruptionEvent(283116);
                        if (_isEndPiercingStrike) { _isEndPiercingStrike = false; StartPiercingStrikeTask(); }
                        break;
                    case 75:
                        UseSkill(20761);
                        if (_isEndFireStorm) { _isEndFireStorm = false; StartFireStormTask(); }
                        break;
                    case 60:
                        UseSkill(20761);
                        break;
                    case 55:
                        LavaEruptionEvent(283118);
                        break;
                    case 40:
                    case 25:
                        UseSkill(20761);
                        break;
                    case 20:
                        CancelTasks();
                        LavaEruptionEvent(283120);
                        break;
                    case 10:
                        UseSkill(20942);
                        break;
                    case 7:
                        UseSkill(20883);
                        Spawn(283045, 679.88f, 1068.88f, 497.88f);
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void LavaEruptionEvent(int floorId)
    {
        Owner.Target = Owner;
        UseSkill(20756);
        if (GetNpc(283051) is null)
        {
            Spawn(283051, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        }
        SpawnFloor(floorId);
    }

    private void SpawnFloor(int floor)
    {
        ScheduleTask(() =>
        {
            Spawn(floor, 679.88f, 1068.88f, 497.88f);
            Spawn(floor + 1, 679.88f, 1068.88f, 497.88f);
        }, 10000);
    }

    private void RndSpawn(int npcId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RndSpawnInRange(npcId, 10);
        }
    }

    private void RndSpawnInRange(int npcId, int dist)
    {
        float direction = System.Random.Shared.Next(0, 200) / 100f;
        float x1 = (float)(Math.Cos(Math.PI * direction) * dist);
        float y1 = (float)(Math.Sin(Math.PI * direction) * dist);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 96, 75, 60, 55, 40, 25, 20, 10, 7 });
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    private void DeleteAdds()
    {
        // note: Java bulk-deleted every add NPC (283116/283118/283120/283051/283045/283257/283237) from
        // the current instance channel. Enumerating an instance's NPCs by id and removing them isn't
        // exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        AddPercent();
        _isHome = true;
        // note: Java reopened instance door 610 here — instance doors aren't exposed to scripts yet.
        base.OnBackHome();
        Owner.RemoveEffectBySkillId(20942);
        DeleteAdds();
        CancelTasks();
        _isEndPiercingStrike = true;
        _isEndFireStorm = true;
    }

    public override void OnDied()
    {
        _percents.Clear();
        // note: Java reopened instance door 610 here — instance doors aren't exposed to scripts yet.
        base.OnDied();
        DeleteAdds();
        CancelTasks();
    }
}
