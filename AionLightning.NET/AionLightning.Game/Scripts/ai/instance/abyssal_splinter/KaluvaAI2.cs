// KaluvaAI2 — Java ai/instance/abyssal_splinter/KaluvaAI2.java. Abyssal Splinter boss: on each
// summon-percentage threshold, spawns an egg at one of 4 fixed points and walks to it to cast a
// hatch skill before resuming the fight.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("kaluva")]
public sealed class KaluvaAI2 : SummonerAI2
{
    // note: Java's handleIndividualSpawnedSummons(Percentage) override — spawned an egg (281902) at a
    // random one of 4 fixed points, disabled thinking, stopped its attack emote, walked to the egg, and
    // broadcast an emote — is a SummonerAI2-specific Java hook with no equivalent virtual on the C#
    // SummonerAI2 base.

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: while its "thinking disabled" flag was set, Java cast a hatch skill (19223) on the egg,
        // then after 2s re-enabled thinking and resumed fighting its most-hated aggressor (or went back
        // to FIGHT/think() if none remained). AggroList and canThink have no C# equivalent.
    }
}
