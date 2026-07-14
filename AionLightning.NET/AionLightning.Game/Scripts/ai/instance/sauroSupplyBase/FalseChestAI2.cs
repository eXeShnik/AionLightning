// FalseChestAI2 — Java ai/instance/sauroSupplyBase/FalseChestAI2.java. Fake chest: spawns a mimic
// NPC on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("false_chest")]
public sealed class FalseChestAI2 : NpcAi2
{
    public override void OnDied()
    {
        Spawn(230843, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        base.OnDied();
        // note: Java also called AI2Actions.deleteOwner(this) to remove the chest immediately after
        // death; deleting an NPC from the world isn't exposed to scripts yet.
    }
}
