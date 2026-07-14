// KlawspawnAI2 — Java ai/worlds/heiron/KlawspawnAI2.java. Has a 10% chance per attack to spawn a
// replacement npc (if not already present) and silently die.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("klawspawn")]
public sealed class KlawspawnAI2 : GeneralNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (GetNpc(212120) is null && Random.Shared.Next(0, 100) < 10)
        {
            Spawn(212120, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
            // note: Java then called AI2Actions.dieSilently(this, creature) to kill the owner without
            // loot/reward; no silent-death hook is exposed to scripts yet.
        }
        // note: Java also overrode canThink() to return false and modifyDamage to clamp to 1; neither
        // hook exists on NpcAi2 yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java also called AI2Actions.deleteOwner(this) here; no owner-delete hook is exposed to
        // scripts yet.
    }
}
