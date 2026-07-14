// SummonRockAI2 — Java ai/instance/padmarashkasCave/SummonRockAI2.java. Summon Rock: casts a rock
// skill on a 5s repeating loop while alive.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("summonrock")]
public sealed class SummonRockAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java looped AI2Actions.useSkill(19180) every 5s via ThreadPoolManager while alive;
        // AI2Actions skill-casting isn't wired at the script layer yet.
    }
}
