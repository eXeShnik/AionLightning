// OneDmgAI2 — Java ai/OneDmgAI2.java. Clamps incoming and outgoing damage to 1.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("one_dmg")]
public sealed class OneDmgAI2 : AggressiveNpcAI2
{
    // note: Java overrode modifyDamage/modifyOwnerDamage to clamp all damage to 1; no damage-modification
    // hook exists on NpcAi2 yet.
}
