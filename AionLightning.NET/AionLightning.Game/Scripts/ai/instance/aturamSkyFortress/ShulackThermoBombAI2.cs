// ShulackThermoBombAI2 — Java ai/instance/aturamSkyFortress/ShulackThermoBombAI2.java. Timed bomb
// prop: detonates 2s after spawn, then self-destructs 4s later.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("shulack_thermo_bomb")]
public sealed class ShulackThermoBombAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a 2s-delayed skill cast (19416) on itself, then self-destructed
        // (AI2Actions.deleteOwner) 4s after that. Also overrode ask(CAN_RESIST_ABNORMAL=POSITIVE) and
        // pollInstance(SHOULD_DECAY/RESPAWN/REWARD=NEGATIVE) — neither poll hook exists on NpcAi2.
    }
}
