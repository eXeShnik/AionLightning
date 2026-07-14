using System;
// ShirikBurrowAI2 — Java ai/instance/muadasTrencher/ShirikBurrowAI2.java. Timed burrow hazard:
// spawns two helper NPCs 25s after spawn then self-deletes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("shirik_burrow")]
public sealed class ShirikBurrowAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                RndSpawn(282535);
                RndSpawn(282535);
                // note: Java despawned the owner via AI2Actions.deleteOwner here; no scripted despawn
                // API exists yet.
            }
        }, 25000);
        base.OnSpawned();
    }

    private void RndSpawn(int npcId)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        int distance = Random.Shared.Next(1, 3);
        float x = Owner.Position.X + MathF.Cos(MathF.PI * direction) * distance;
        float y = Owner.Position.Y + MathF.Sin(MathF.PI * direction) * distance;
        Spawn(npcId, x, y, Owner.Position.Z);
        // note: Java also shouted 1500307 from the spawned helper NPC (NpcShoutsService with an
        // explicit target); shouting from a non-owner NPC isn't exposed via the SendMsg helper.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java called AI2Actions.deleteOwner(this) here too; also overrode canThink()/ask()/
        // pollInstance() to stay always-active, resist abnormals, and refuse decay/respawn/reward —
        // the AI2 poll/gating framework has no C# equivalent.
    }
}
