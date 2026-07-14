// PortalRequestAI2 — Java ai/portals/PortalRequestAI2.java. Portal that asks the player to confirm a
// paid teleport before moving them.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("portal_request")]
public sealed class PortalRequestAI2 : PortalAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java resolved the teleport template's first TeleLocation/price via
        // DataManager.TELELOCATION_DATA and PricesService, then opened a paid-teleport confirmation via
        // SM_QUESTION_WINDOW + RequestResponseHandler that teleported on accept through
        // TeleportService2.teleport. None of the teleport-template resolution, response-requester flow,
        // or pricing service are reachable from the script layer.
    }
}
