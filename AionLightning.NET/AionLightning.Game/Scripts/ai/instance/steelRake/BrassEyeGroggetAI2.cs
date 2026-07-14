// BrassEyeGroggetAI2 — Java ai/instance/steelRake/BrassEyeGroggetAI2.java. Steel Rake boss: was
// meant to spawn HP-percentage-gated helper waves around fixed room coordinates.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("brasseyegrogget")]
public sealed class BrassEyeGroggetAI2 : SummonerAI2
{
    // note: Java's handleSpawned only called super.handleSpawned() (no extra body), and its
    // handleIndividualSpawnedSummons(Percentage) — the per-threshold helper-wave spawner (coordinated
    // via ThreadPoolManager, spawning npcs 281181-281187 in room-fixed positions) — overrides an
    // ai2-framework percentage-threshold hook that has no equivalent on NpcAi2; SummonerAI2's own
    // percentage-threshold tracking isn't wired at the script layer yet either, so there is nothing to
    // override here.
}
