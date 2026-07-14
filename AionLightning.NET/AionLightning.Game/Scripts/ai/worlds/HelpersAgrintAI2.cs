// HelpersAgrintAI2 — Java ai/worlds/HelpersAgrintAI2.java. Agrint's helper adds: only clamp incoming/
// outgoing damage to 1.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("helpers_agrint")]
public sealed class HelpersAgrintAI2 : AggressiveNpcAI2
{
    // note: Java overrode modifyDamage/modifyOwnerDamage to clamp all damage to 1; no damage-modification
    // hook exists on NpcAi2 yet.
}
