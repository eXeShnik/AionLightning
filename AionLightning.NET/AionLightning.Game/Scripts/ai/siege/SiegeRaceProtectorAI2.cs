// SiegeRaceProtectorAI2 — Java ai/siege/SiegeRaceProtectorAI2.java. Marker AI whose only Java
// behaviour was its pollInstance override; has no C# equivalent.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("siege_raceprotector")]
public sealed class SiegeRaceProtectorAI2 : SiegeNpcAI2
{
    // note: Java's pollInstance (SHOULD_DECAY -> POSITIVE, SHOULD_RESPAWN -> NEGATIVE, SHOULD_REWARD ->
    // POSITIVE) has no C# equivalent — AIQuestion polling isn't exposed to the script layer.
}
