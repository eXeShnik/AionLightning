// JollyMakekikeAI2 — Java ai/instance/nightmareCircus/JollyMakekikeAI2.java. Circus boss that rings
// a helper-spawn ring around a fixed "ui" npc at HP thresholds, then replaces surviving helpers with
// aggro'd servants once they're cleared.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("jolly_makekike")]
public sealed class JollyMakekikeAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private readonly List<Npc> _helpers = new();
    private readonly List<Npc> _servants = new();
    private int _lastSpawn;

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
        Sp(831348);
        _lastSpawn = 831348;
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
                // note: Java set canThink=false here to pause the think loop until the spawn window
                // closed; the think-loop gate has no NpcAi2 equivalent.
                ScheduleTask(() =>
                {
                    Sp(Random.Shared.Next(831348, 831350));
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
        RemoveHelpers();
        var ui = GetUi();
        if (ui is not null)
        {
            const int amount = 12;
            float interval = (float)(Math.PI * 2.0 / (amount / 2));
            int h = ui.Position.Heading - 60;
            if (h < 0) h = 120 + h;
            for (int i = 0; i < amount; i++)
            {
                float x1 = (float)(Math.Cos(interval * i) * 17);
                float y1 = (float)(Math.Sin(interval * i) * 17);
                var helper = Spawn(npcId, ui.Position.X + x1, ui.Position.Y + y1, 199.50775f, (byte)h);
                if (helper is not null) _helpers.Add(helper);
            }
        }

        ScheduleTask(RemoveHelpers, 15000);
        _lastSpawn = npcId;
        SendMsg(1500998);
    }

    private Npc? GetUi() => GetNpc(831573) ?? GetNpc(831741);

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 65, 45, 30 });
    }

    public override void OnDespawned()
    {
        CancelTasks(); // note: Java only cancelled its own think-task; this AI schedules nothing else.
        base.OnDespawned();
    }

    private void RemoveHelpers()
    {
        // note: Java despawned each ring helper (obj.getController().onDelete()), then for npc ids
        // 831348/831349 replaced it with a "servant" (233152/233148) that got an NpcShoutsService
        // shout and aggro on a random player in the instance (via getAggroList().addHate); NPC
        // despawn and cross-npc aggro-list access aren't wired at the script layer yet.
        _helpers.Clear();
        SendMsg(_lastSpawn == 831348 ? 1500999 : 1501000);
    }

    private void RemoveServant()
    {
        // note: same despawn-unavailability as RemoveHelpers, for the servant list.
        _servants.Clear();
    }

    public override void OnDied()
    {
        RemoveHelpers();
        RemoveServant();
        base.OnDied(); // cancels the scheduled think-task
    }

    public override void OnBackHome()
    {
        SendMsg(1500994);
        AddPercent();
        CancelTasks();
        RemoveHelpers();
        RemoveServant();
        base.OnBackHome();
    }

    public void OwnerSkillUse(int skillId)
    {
        if (skillId == 21345) SendMsg(1500990);
    }
}
