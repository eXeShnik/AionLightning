// GoldenTatarCloneAI2 — Java ai/worlds/tiamaranta/GoldenTatarCloneAI2.java. Only overrode the
// AI-question poll (resist abnormal, refuse everything else).
using AionLightning.Game.Ai;

namespace Ai;

[AiName("golden_tatar_clone")]
public sealed class GoldenTatarCloneAI2 : AggressiveNpcAI2
{
    // note: Java overrode ask(AIQuestion) to resist CAN_RESIST_ABNORMAL and refuse everything else; no
    // AI-question poll hook exists on NpcAi2 yet.
}
