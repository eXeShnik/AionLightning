// SuspiciousCannonAI2 — Java ai/instance/steelRake/SuspiciousCannonAI2.java. Use-item cannon that
// flight-teleports the player who uses it.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("suspiciouscannon")]
public sealed class SuspiciousCannonAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java set CreatureState.FLIGHT_TELEPORT/unset ACTIVE, set flightTeleportId(73001), and
        // broadcast SM_EMOTION(START_FLYTELEPORT); flight-teleport state and packets aren't wired at
        // the script layer yet.
    }
}
