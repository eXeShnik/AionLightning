using System.Linq;
using System.Collections.Generic;
using System;
// CanyonFragmentAI2 — Java ai/instance/elementisForest/CanyonFragmentAI2.java. Spawns a follow-up
// npc 25s after appearing, unless killed first.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("canyonfragment")]
public sealed class CanyonFragmentAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        Schedule();
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    private void Schedule()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                Spawn(282430, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
                // note: Java then deleted itself via AI2Actions.deleteOwner; no scripted despawn API
                // exists yet.
            }
        }, 25000);
    }
}
