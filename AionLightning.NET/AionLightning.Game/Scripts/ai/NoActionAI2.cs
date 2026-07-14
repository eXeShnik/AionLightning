// Proof script for the ai2 foundation: a fully inert AI bound to the "noaction" ai-name, mirroring
// Java's NoActionAI2 (data/scripts/system/handlers/ai/NoActionAI2.java). Exists so
// AiEngineHostedService discovers at least one script at boot; behavior is intentionally a no-op —
// dummy/decoration NPCs already get that from NpcAiService's NoAction archetype gating.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("noaction")]
public class NoActionAI2 : NpcAi2
{
}
