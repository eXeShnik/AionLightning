// SuspiciousCannonAI2 — Java ai/instance/steelRose/SuspiciousCannonAI2.java. Use-item cannon that
// flight-teleports the player to a hardcoded destination.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai.SteelRose;

[AiName("suspiciouscannonroza")]
public sealed class SuspiciousCannonAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java set the player into FLIGHT_TELEPORT state, cleared ACTIVE, stored a flight-teleport
        // destination id (73001), and broadcast an SM_EMOTION(START_FLYTELEPORT) packet. Flight-teleport
        // state/ids and PacketSendUtility aren't exposed to the script layer yet.
    }
}
