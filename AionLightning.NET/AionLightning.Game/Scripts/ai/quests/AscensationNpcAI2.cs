// AscensationNpcAI2 — Java ai/quests/AscensationNpcAI2.java. Ascension quest NPC that clamps its
// outgoing damage to 1.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("ascensationquestnpc")]
public sealed class AscensationNpcAI2 : AggressiveNpcAI2
{
    // note: Java overrode modifyOwnerDamage to clamp outgoing damage to 1; no damage-modification hook
    // exists on NpcAi2 yet (same gap as OneDmgAI2's modifyDamage/modifyOwnerDamage).
}
