// PortalElevatorAI2 — Java ai/portals/PortalElevatorAI2.java. Portal that plays an elevator emote
// before delegating to the base portal teleport.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("portal_elevator")]
public sealed class PortalElevatorAI2 : PortalAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java broadcast an SM_EMOTION(EMOTE, 144) elevator animation before delegating —
        // SM_EMOTION broadcasting isn't reachable from the script layer.
        base.HandleUseItemFinish(player);
    }
}
