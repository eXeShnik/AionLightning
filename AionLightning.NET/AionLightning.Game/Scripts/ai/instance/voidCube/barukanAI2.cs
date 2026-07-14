// barukanAI2 — Java ai/instance/voidCube/barukanAI2.java. Void Cube boss: at 50% HP, spawns two adds
// (230092) near itself; despawns them again on death/back-home.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("barukan")]
public sealed class barukanAI2 : AggressiveNpcAI2
{
    private bool _addsSpawned;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_addsSpawned || Owner.HpPercentage > 50) return;
        _addsSpawned = true;
        double direction = Random.Shared.Next(0, 200) / 100.0;
        int distance = Random.Shared.Next(1, 4);
        float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
        float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
        Spawn(230092, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
        Spawn(230092, Owner.Position.X + y1, Owner.Position.Y + x1, Owner.Position.Z);
        // note: Java gated this via a percents list checked from a canThink()-guarded thread; canThink()
        // has no C# equivalent, so the 50% trigger here simply fires once per encounter.
    }

    public override void OnDied()
    {
        base.OnDied();
        _addsSpawned = false;
        // note: Java despawned the 230092 adds here via WorldMapInstance.getNpcs + Npc.getController().onDelete;
        // no despawn-by-npcId helper exists on NpcAi2 yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _addsSpawned = false;
        // note: Java despawned the 230092 adds here too (same as OnDied).
    }
}
