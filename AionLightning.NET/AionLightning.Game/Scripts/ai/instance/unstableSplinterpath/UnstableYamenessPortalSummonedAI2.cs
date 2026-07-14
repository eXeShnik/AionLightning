using System.Linq;
using System.Collections.Generic;
using System;
// UnstableYamenessPortalSummonedAI2 — Java ai/instance/unstableSplinterpath/UnstableYamenessPortalSummonedAI2.java.
// Portal add: spawns a pair of helpers 12s after appearing, then another pair every 60s after that.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("unstableyamenessportal")]
public sealed class UnstableYamenessPortalSummonedAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        ScheduleTask(SpawnSummons, 12000);
    }

    private void SpawnSummons()
    {
        Spawn(219565, Owner.Position.X + 3, Owner.Position.Y - 3, Owner.Position.Z);
        Spawn(219566, Owner.Position.X - 3, Owner.Position.Y + 3, Owner.Position.Z);
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                Spawn(219565, Owner.Position.X + 3, Owner.Position.Y - 3, Owner.Position.Z);
                Spawn(219566, Owner.Position.X - 3, Owner.Position.Y + 3, Owner.Position.Z);
            }
        }, 60000);
    }
}
