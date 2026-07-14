// EternalFlameAI2 — Java ai/worlds/theobomos/EternalFlameAI2.java. Spawns a replacement npc slightly
// below its own position on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("eternal_flame")]
public sealed class EternalFlameAI2 : NpcAi2
{
    public override void OnDied()
    {
        Spawn(214552, Owner.Position.X, Owner.Position.Y, Owner.Position.Z - 3);
        base.OnDied();
        // note: Java then called AI2Actions.deleteOwner(this) and overrode modifyDamage to clamp incoming
        // damage to 1; neither owner-delete nor damage-modification hooks exist on NpcAi2 yet.
    }
}
