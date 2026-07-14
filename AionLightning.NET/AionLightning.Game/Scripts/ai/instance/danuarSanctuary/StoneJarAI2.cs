// StoneJarAI2 — Java ai/instance/danuarSanctuary/StoneJarAI2.java. Stone jar prop: on death, has a
// 1-in-3 chance to spawn a remains prop at its own position.
using System;
using AionLightning.Game.Ai;

namespace Ai;

[AiName("stone_jar")]
public sealed class StoneJarAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        if (Random.Shared.Next(1, 4) == 1)
        {
            SpawnRemains();
        }
    }

    private void SpawnRemains()
    {
        var pos = getOwner().Position;
        Spawn(284026, pos.X, pos.Y, pos.Z);
        // note: Java also deleted itself via AI2Actions.deleteOwner; AI2Actions isn't exposed to scripts yet.
    }
}
