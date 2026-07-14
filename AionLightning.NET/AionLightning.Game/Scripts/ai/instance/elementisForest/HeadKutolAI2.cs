using System.Linq;
using System.Collections.Generic;
using System;
// HeadKutolAI2 — Java ai/instance/elementisForest/HeadKutolAI2.java. Rarely spawns 1-3 clones of
// itself on attack (Java's own roll of Rnd.get(1,100) < 1 never actually succeeds).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kutol")]
public sealed class HeadKutolAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);

        if (Random.Shared.Next(1, 101) < 1)
            SpawnClone();
    }

    private void SpawnClone()
    {
        var kutolClone = GetNpc(282302);
        int random = Random.Shared.Next(1, 4);
        if (kutolClone is null)
        {
            switch (random)
            {
                case 1:
                    Spawn(282302, Owner.Position.X, Owner.Position.Y, Owner.Position.Z + 2, 3);
                    break;
                case 2:
                    Spawn(282302, Owner.Position.X, Owner.Position.Y, Owner.Position.Z + 2, 3);
                    Spawn(282302, Owner.Position.X - 5, Owner.Position.Y - 3, Owner.Position.Z + 2, 3);
                    break;
                default:
                    Spawn(282302, Owner.Position.X, Owner.Position.Y, Owner.Position.Z + 2, 3);
                    Spawn(282302, Owner.Position.X - 5, Owner.Position.Y - 3, Owner.Position.Z + 2, 3);
                    Spawn(282302, Owner.Position.X + 5, Owner.Position.Y - 3, Owner.Position.Z + 2, 3);
                    break;
            }
        }
    }
}
