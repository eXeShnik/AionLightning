// StoneJarIdleAI2 — Java ai/instance/idgelResearchCenter/StoneJarIdleAI2.java. Idle stone jar prop:
// on death, has a 1-in-3 chance to spawn a ring of remains around itself.
using System;
using AionLightning.Game.Ai;

namespace Ai;

[AiName("stone_jar_idle")]
public sealed class StoneJarIdleAI2 : NpcAi2
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
        Spawn(284026, pos.X + 1, pos.Y + 1, pos.Z);
        Spawn(284026, pos.X + 2, pos.Y + 2, pos.Z);
        Spawn(284026, pos.X, pos.Y, pos.Z);
        Spawn(284026, pos.X - 1, pos.Y - 1, pos.Z);
        Spawn(284026, pos.X - 2, pos.Y - 2, pos.Z);
        // note: Java also deleted itself via AI2Actions.deleteOwner; AI2Actions isn't exposed to scripts yet.
    }
}
