// DanariaSiegeGateAI2 — Java ai/siege/DanariaSiegeGateAI2.java. Danaria outer-gate use-item NPC:
// using it despawns the matching inner-gate NPC and notifies the player.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("danaria_siege_gate")]
public sealed class DanariaSiegeGateAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java despawned the matching inner-gate NPC (found via known-list, by npc id: 701783 ->
        // 273286, 701784 -> 273289, 701785 -> 273285, 701786 -> 273288) and sent an SM_SYSTEM_MESSAGE
        // door-despawn notice. NPC despawn-by-id and PacketSendUtility aren't exposed to the script layer yet.
    }
}
