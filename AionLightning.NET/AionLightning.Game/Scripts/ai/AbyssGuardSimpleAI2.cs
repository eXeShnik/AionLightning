// AbyssGuardSimpleAI2 — Java ai/AbyssGuardSimpleAI2.java. Simplified abyss-guard NPC AI.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("simple_abyssguard")]
public sealed class AbyssGuardSimpleAI2 : AggressiveNpcAI2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java delegated to SimpleAbyssGuardHandler.onCreatureSee.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java delegated to SimpleAbyssGuardHandler.onCreatureMoved (also overrode
        // handleGuardAgainstAttacker to always return false and canHandleEvent for state gating —
        // both dropped, no C# equivalents).
    }
}
