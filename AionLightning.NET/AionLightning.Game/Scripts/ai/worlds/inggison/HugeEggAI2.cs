// HugeEggAI2 — Java ai/worlds/inggison/HugeEggAI2.java. Has a 50% chance to hatch a replacement npc
// on death.
using System;
using AionLightning.Game.Ai;

namespace Ai;

[AiName("hugeegg")]
public sealed class HugeEggAI2 : GeneralNpcAI2
{
    public override void OnDied()
    {
        base.OnDied();
        if (Random.Shared.Next(0, 100) < 50)
        {
            Spawn(217097, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
            // note: Java then called AI2Actions.deleteOwner(this); no owner-delete hook is exposed to
            // scripts yet.
        }
        // note: Java also overrode canThink() to return false and modifyDamage to clamp to 1; neither
        // hook exists on NpcAi2 yet.
    }
}
