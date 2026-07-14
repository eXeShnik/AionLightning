// OmegaAI2 — Java ai/worlds/inggison/OmegaAI2.java. Summoner variant: casts skills before summoning
// and requires a nearby player to summon at all.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("omega")]
public sealed class OmegaAI2 : SummonerAI2
{
    // note: Java overrode handleBeforeSpawn (cast skills 19189/19191 before summoning), handleSpawnFinished
    // (cast 18671 once the 281948 summon group finished), and checkBeforeSpawn (required a known player
    // within 30m before summoning); SummonerAI2's percentage/summon-group template hooks aren't wired to
    // scripts yet (see SummonerAI2's own notes).
}
