// SummonedPletusAI2 — Java ai/instance/argentManor/SummonedPletusAI2.java. Summoned add whose
// own think loop is disabled (behaviour driven externally by its summoner).
using AionLightning.Game.Ai;

namespace Ai;

[AiName("summoned_pletus")]
public sealed class SummonedPletusAI2 : AggressiveNpcAI2
{
    // note: Java's only override was canThink() => false, which has no C# equivalent — NPC think is
    // owned by NpcAiService regardless.
}
