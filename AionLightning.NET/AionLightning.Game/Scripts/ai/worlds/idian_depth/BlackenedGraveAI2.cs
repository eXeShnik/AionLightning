// BlackenedGraveAI2 — Java ai/worlds/idian_depth/BlackenedGraveAI2.java. Spawns a fixed replacement
// npc on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("blackened_grave")]
public sealed class BlackenedGraveAI2 : NpcAi2
{
    public override void OnDied()
    {
        Spawn(284262, 394.15338f, 893.5626f, 559.375f);
        base.OnDied();
        // note: Java then called AI2Actions.deleteOwner(this) and overrode modifyDamage to clamp incoming
        // damage to 1; neither owner-delete nor damage-modification hooks exist on NpcAi2 yet.
    }
}
