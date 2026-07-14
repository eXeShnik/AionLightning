// SteelRoseCargoDeckMobileCannonAI2 — Java ai/instance/steelRose/SteelRoseCargoDeckMobileCannonAI2.java.
// Cargo-deck cannon use-item: consumes a key item then silently kills two hardcoded instance NPCs.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("steelrosedeckmobilecannon")]
public sealed class SteelRoseCargoDeckMobileCannonAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java consumed one item 185000052 from the player's inventory (bailing with an
        // SM_SYSTEM_MESSAGE on failure), then — only inside instance map 301050000 — silently killed
        // every NPC 230727/231460 in the instance via WorldMapInstance.getNpcs + AI2Actions.killSilently.
        // Inventory item consumption, per-instance NPC lookups, and silent-kill aren't exposed yet.
    }
}
