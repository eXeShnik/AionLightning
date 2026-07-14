// StudioPortalAI2 — Java ai/portals/StudioPortalAI2.java. Player-housing studio entrance/exit
// portal: teleports the player into their studio instance, or back out if already inside one.
// note: Java's onDialogSelect always returned true (no-op) — no NpcAi2 hook to override.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("studioportal")]
public sealed class StudioPortalAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java resolved the player's current world-map-instance owner id and their House via
        // HousingService, then teleported in/out of the studio instance via InstanceService/
        // TeleportService2 (or sent STR_HOUSING_ENTER_NEED_HOUSE when they own no studio). House,
        // HousingService and the studio instance-resolution flow aren't reachable from the script layer.
    }
}
