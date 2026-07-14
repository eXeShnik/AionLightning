// AbyssalSplinterPortalAI2 — Java ai/portals/abyssalsplinter/AbyssalSplinterPortalAI2.java.
// Teleportation device that routes to one of three fixed destinations based on the NPC's own spawn X.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("teleportation_device")]
public sealed class AbyssalSplinterPortalAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        float x = Owner.Position.X;
        if (x == 302.201f)
        {
            // note: Java teleported to 300220000 (294.632, 732.189, 215.854) via TeleportService2 —
            // TeleportService isn't reachable from the script layer.
        }
        else if (x == 334.001f)
        {
            // note: Java teleported to 300220000 (338.475, 701.417, 215.916).
        }
        else if (x == 362.192f)
        {
            // note: Java teleported to 300220000 (373.611, 739.125, 215.903).
        }
    }
}
