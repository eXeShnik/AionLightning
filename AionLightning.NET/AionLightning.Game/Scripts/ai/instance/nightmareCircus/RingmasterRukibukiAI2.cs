// RingmasterRukibukiAI2 — Java ai/instance/nightmareCircus/RingmasterRukibukiAI2.java. Circus boss
// that spawns a fixed mammoth quartet or a random nearby helper at HP thresholds.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ringmaster_rukibuki")]
public sealed class RingmasterRukibukiAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
        SendMsg(1500983);
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
                    int npcId = Random.Shared.Next(100) < 50 ? 233162 : 233151;
                    Sp(npcId);
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
        if (npcId == 233151)
        {
            Spawn(npcId, 521.58502f, 510.16528f, 199.59279f, 30);
            Spawn(npcId, 521.71558f, 505.26691f, 199.50775f, 30);
            Spawn(npcId, 523.37469f, 621.13623f, 208.05113f, 90);
            Spawn(npcId, 523.67548f, 616.54547f, 208.05113f, 90);
            // note: Java then aggro'd each spawned mammoth on a random player in the instance
            // (getAggroList().addHate); cross-npc aggro-list access isn't wired at the script layer
            // yet.
            SendMsg(1500992);
            return;
        }

        float direction = Random.Shared.Next(0, 200) / 100f;
        int distance = Random.Shared.Next(1, 3);
        float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
        float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
        var npc = Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z, (byte)Owner.Position.Heading);
        _ = npc; // note: Java also had the spawned npc shout via NpcShoutsService; not wired at the script layer yet.
        SendMsg(1500991);
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
        // note: Java despawned every instance npc 233162/233151 (WorldMapInstance.getNpcs + controller
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
