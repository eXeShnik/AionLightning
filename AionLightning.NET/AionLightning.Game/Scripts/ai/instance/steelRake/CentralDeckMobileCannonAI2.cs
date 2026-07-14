// CentralDeckMobileCannonAI2 — Java ai/instance/steelRake/CentralDeckMobileCannonAI2.java. Use-item
// cannon that consumes an item and silently kills four fixed instance NPCs.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("centralcannon")]
public sealed class CentralDeckMobileCannonAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java decreased item 185000052 by 1 (bailing with SM_SYSTEM_MESSAGE 1111302 on failure)
        // before silently killing npcs 215402-215405 via AI2Actions.killSilently; player inventory
        // mutation, system messages, and silent-kill aren't wired at the script layer yet.
    }
}
