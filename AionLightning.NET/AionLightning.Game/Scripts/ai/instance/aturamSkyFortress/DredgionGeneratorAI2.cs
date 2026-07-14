// DredgionGeneratorAI2 — Java ai/instance/aturamSkyFortress/DredgionGeneratorAI2.java. Inert
// environmental prop with its own think loop disabled.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dredgion_generator")]
public sealed class DredgionGeneratorAI2 : GeneralNpcAI2
{
    // note: Java's only overrides were canThink() => false (no C# equivalent) and
    // ask(CAN_RESIST_ABNORMAL=POSITIVE) — that poll hook doesn't exist on NpcAi2.
}
