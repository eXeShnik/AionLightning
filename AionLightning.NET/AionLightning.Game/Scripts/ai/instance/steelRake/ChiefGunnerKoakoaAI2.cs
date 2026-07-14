// ChiefGunnerKoakoaAI2 — Java ai/instance/steelRake/ChiefGunnerKoakoaAI2.java. Steel Rake boss that
// spawns 1-3 waves of helper cannons around fixed room coordinates at HP thresholds.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("gunnerkoakoa")]
public sealed class ChiefGunnerKoakoaAI2 : SummonerAI2
{
    // note: Java's handleIndividualSpawnedSummons(Percentage) — the per-threshold helper-cannon
    // spawner (npcs 281212/281213 in fixed room positions, gated behind abnormal effect 18552) —
    // overrides an ai2-framework percentage-threshold hook with no NpcAi2 equivalent; SummonerAI2's
    // own percentage tracking isn't wired at the script layer yet either, so there is nothing to
    // override here.
}
