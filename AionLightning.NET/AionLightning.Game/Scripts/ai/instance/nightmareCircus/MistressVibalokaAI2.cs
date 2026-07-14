// MistressVibalokaAI2 — Java ai/instance/nightmareCircus/MistressVibalokaAI2.java. Circus boss that
// spawns a burst of helper npcs near itself at HP thresholds.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("mistress_vibaloka")]
public sealed class MistressVibalokaAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
        SendMsg(1500988);
    }

    // note: Java's canThink() override has no NpcAi2 equivalent — dropped.

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
                _percents.Remove(percent);
                ScheduleTask(() =>
                {
                    int count = Random.Shared.Next(2, 5);
                    while (count > 0)
                    {
                        count--;
                        Sp(233150);
                    }
                    SendMsg(1500987);
                    // note: Java re-targeted the most-hated aggro entry (getAggroList/getMoveController/
                    // GameStats renew calls) once the spawn window closed; aggro-list access isn't
                    // wired at the script layer yet.
                }, 2000);
                break;
            }
        }
    }

    private void Sp(int npcId)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        int distance = Random.Shared.Next(1, 3);
        float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
        float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z, (byte)Owner.Position.Heading);
        // note: Java scheduled the spawned helper's deletion after 15s (NpcActions.delete); NPC
        // despawn isn't wired at the script layer yet.
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 85, 65, 55, 45, 30, 15 });
    }

    public override void OnDespawned()
    {
        CancelTasks(); // note: Java only cancelled its own think-task; this AI schedules nothing else.
        base.OnDespawned();
    }

    private void RemoveHelpers()
    {
        // note: Java despawned every instance npc 233150 (WorldMapInstance.getNpcs + controller
        // onDelete); enumerating/despawning other npcs by id isn't wired at the script layer yet.
    }

    public override void OnDied()
    {
        RemoveHelpers();
        base.OnDied(); // cancels the scheduled think-task
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTasks();
        RemoveHelpers();
        base.OnBackHome();
    }
}
