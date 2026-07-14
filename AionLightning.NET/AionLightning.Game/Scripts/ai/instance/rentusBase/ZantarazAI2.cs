// ZantarazAI2 — Java ai/instance/rentusBase/ZantarazAI2.java. Rentus Base boss: HP-breakpoint add-spawn
// event around the boss.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("zantaraz")]
public sealed class ZantarazAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                _percents.Remove(percent);
                for (int i = 0; i < 4; i++) RndSpawn(282384);
                for (int i = 0; i < 4; i++) RndSpawn(217301);
                break;
            }
        }
    }

    private void RndSpawn(int npcId)
    {
        double direction = Math.PI * (System.Random.Shared.Next(0, 200) / 100.0);
        int distance = System.Random.Shared.Next(0, 3);
        var p = Owner.Position;
        float x = p.X + (float)(Math.Cos(direction) * distance);
        float y = p.Y + (float)(Math.Sin(direction) * distance);
        Spawn(npcId, x, y, p.Z, (byte)p.Heading);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied();
        // note: Java also deleted the summoned adds (282384/217301) and the instance markers
        // (218610/218611) via WorldMapInstance.getNpcs — NPC listing/delete isn't exposed to scripts yet.
    }
}
