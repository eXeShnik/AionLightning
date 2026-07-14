// SpeakerAI2 — Java ai/SpeakerAI2.java. Siege-status town-crier NPC: answers idle shout patterns
// with race/world-specific siege-progress checks.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("speaker")]
public sealed class SpeakerAI2 : GeneralNpcAI2
{
    // note: Java's onPatternShout(ShoutEventType, pattern, skillNumber) gated idle-shout patterns on
    // SiegeService.isSiegeInProgress/getSiegeLocation per race/world/pattern number. That shout-pattern
    // hook and SiegeService aren't ported to the script layer yet.
}
