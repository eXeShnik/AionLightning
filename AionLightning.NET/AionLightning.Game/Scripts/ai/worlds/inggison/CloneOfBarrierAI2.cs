// CloneOfBarrierAI2 — Java ai/worlds/inggison/CloneOfBarrierAI2.java. On death, removes a shield
// effect from a nearby barrier npc.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("omegaclone")]
public sealed class CloneOfBarrierAI2 : AggressiveNpcAI2
{
    public override void OnDied()
    {
        // note: Java scanned its known-list for a living npc 216516 within 5m and removed effect 18671
        // from it; known-list iteration and effect removal by id aren't exposed to scripts yet.
        base.OnDied();
    }
}
