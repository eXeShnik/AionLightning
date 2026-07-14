using System.Linq;
using System.Collections.Generic;
using System;
// MosquaEggAI2 — Java ai/instance/tallocsHollow/MosquaEggAI2.java. Egg NPC that hatches into a
// Supraklaw add after a delay, then self-deletes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("mosquaegg")]
public sealed class MosquaEggAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(CheckSpawn, 17000);
    }

    private void CheckSpawn()
    {
        // note: Java also gated this on getPosition().isSpawned(); world-presence isn't exposed to
        // scripts yet.
        Spawn(217132, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
        // note: Java deleted itself via AI2Actions.deleteOwner after hatching; no scripted despawn API
        // exists yet.
    }
}
