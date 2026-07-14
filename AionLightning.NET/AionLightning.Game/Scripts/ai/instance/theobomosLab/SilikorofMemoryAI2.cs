// SilikorofMemoryAI2 — Java ai/instance/theobomosLab/SilikorofMemoryAI2.java. Silikorof Memory boss:
// spawns two adds (281054/281053) the first time its HP crosses 50/25/10%; despawns them on death.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("silikor")]
public sealed class SilikorofMemoryAI2 : AggressiveNpcAI2
{
    private static readonly int[] Thresholds = { 50, 25, 10 };
    private int _nextThreshold;

    public override void OnSpawned()
    {
        base.OnSpawned();
        _nextThreshold = 0;
        // note: Java also cast skill 18481 directly on spawn for npcId 214668; skill-engine casting isn't
        // wired at the script layer yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_nextThreshold >= Thresholds.Length || Owner.HpPercentage > Thresholds[_nextThreshold]) return;
        _nextThreshold++;
        SpawnNear(281054);
        SpawnNear(281053);
    }

    private void SpawnNear(int npcId)
    {
        double direction = Random.Shared.Next(0, 200) / 100.0;
        int distance = Random.Shared.Next(0, 3);
        float x = (float)(Math.Cos(Math.PI * direction) * distance);
        float y = (float)(Math.Sin(Math.PI * direction) * distance);
        Spawn(npcId, Owner.Position.X + x, Owner.Position.Y + y, Owner.Position.Z, (byte)Owner.Position.Heading);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _nextThreshold = 0;
    }

    public override void OnDespawned()
    {
        _nextThreshold = Thresholds.Length;
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied();
        _nextThreshold = Thresholds.Length;
        // note: Java despawned the 281054/281053 adds here via WorldMapInstance.getNpcs +
        // Npc.getController().onDelete; no despawn-by-npcId helper exists on NpcAi2 yet.
    }
}
