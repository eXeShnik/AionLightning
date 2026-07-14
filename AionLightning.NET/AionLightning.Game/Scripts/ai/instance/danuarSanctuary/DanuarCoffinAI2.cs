// DanuarCoffinAI2 — Java ai/instance/danuarSanctuary/DanuarCoffinAI2.java. Coffin prop: on death,
// has a 1-in-2 chance to spawn a remains prop at its own position.
using System;
using AionLightning.Game.Ai;

namespace Ai;

[AiName("danuar_coffin")]
public sealed class DanuarCoffinAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        if (Random.Shared.Next(1, 3) == 1)
        {
            SpawnRemains();
        }
    }

    private void SpawnRemains()
    {
        var pos = getOwner().Position;
        Spawn(233085, pos.X, pos.Y, pos.Z);
        // note: Java also deleted itself via AI2Actions.deleteOwner; AI2Actions isn't exposed to scripts yet.
    }
}
