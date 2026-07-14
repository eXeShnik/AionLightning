using System.Linq;
using System.Collections.Generic;
using System;
// SeedHetgolemAI2 — Java ai/instance/elementisForest/SeedHetgolemAI2.java. On death, transforms
// back into its restored-golem form.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("seed_hetgolem")]
public sealed class SeedHetgolemAI2 : AggressiveNpcAI2
{
    public override void OnDied()
    {
        Spawn(282441, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
        Spawn(282465, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
        // note: Java deleted the smoke npc (282465) via NpcActions.delete immediately, then deleted
        // itself via AI2Actions.deleteOwner after calling super.handleDied(); neither despawn path is
        // exposed to scripts yet.
        base.OnDied();
    }
}
