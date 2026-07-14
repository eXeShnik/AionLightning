// TiamarantaPortalsAI2 — Java ai/portals/TiamarantaPortalsAI2.java. Tiamaranta portal that only
// opens once the player's faction holds at least 2 siege source locations.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tiamarantaportal")]
public sealed class TiamarantaPortalsAI2 : PortalAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java counted SiegeService.getInstance().getSources() entries matching the player's race
        // and only delegated to the base portal teleport when that count was >= 2. SiegeService isn't
        // reachable from the script layer.
    }
}
