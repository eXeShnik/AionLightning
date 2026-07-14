// SiegeNpcAI2 — Java ai/siege/SiegeNpcAI2.java. Common siege-NPC root: every dedicated siege
// script (protectors, mines, shields, teleporters, etc.) extends this instead of AggressiveNpcAI2
// directly.
using AionLightning.Game.Ai;

namespace Ai;

public class SiegeNpcAI2 : AggressiveNpcAI2
{
    // note: Java's pollInstance (SHOULD_DECAY/SHOULD_RESPAWN -> NEGATIVE, SHOULD_REWARD -> POSITIVE) and
    // getSpawnTemplate (cast to SiegeSpawnTemplate) have no C# equivalent — AIQuestion polling and
    // SiegeSpawnTemplate aren't exposed to the script layer.
}
