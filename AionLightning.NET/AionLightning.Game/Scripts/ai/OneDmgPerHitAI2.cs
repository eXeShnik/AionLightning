// OneDmgPerHitAI2 — Java ai/OneDmgPerHitAI2.java. Inert NPC (NoActionAI2) that also clamps
// incoming damage to 1.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("onedmgperhit")]
public class OneDmgPerHitAI2 : NoActionAI2
{
    // note: Java overrode modifyDamage to clamp incoming damage to 1; no damage-modification hook exists
    // on NpcAi2 yet.
}
