// BalaurBarricadeAI2 — Java ai/instance/darkPoeta/BalaurBarricadeAI2.java. Barricade npc that spawns
// a pair of helper npcs near itself at two HP thresholds.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("balaurbarricade")]
public sealed class BalaurBarricadeAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    // note: Java's modifyDamage(int) override (always returns 1) has no NpcAi2 equivalent — dropped.

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
                switch (percent)
                {
                    case 60:
                    case 10:
                        Sp();
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void Sp()
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        int distance = Random.Shared.Next(1, 5);
        float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
        float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
        int npcId = Owner.Template.NpcId;
        if (npcId == 700517 || npcId == 700556)
        {
            Spawn(215262, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
            Spawn(215262, Owner.Position.X + y1, Owner.Position.Y + x1, Owner.Position.Z);
        }
        else if (npcId == 700558)
        {
            Spawn(215262, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
            Spawn(214883, Owner.Position.X + y1, Owner.Position.Y + x1, Owner.Position.Z);
        }
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 60, 10 });
    }

    public override void OnSpawned()
    {
        AddPercent();
        // note: Java called super.handleDespawned() here instead of super.handleSpawned() (likely a
        // copy-paste bug in the original) — preserved verbatim for fidelity.
        base.OnDespawned();
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
