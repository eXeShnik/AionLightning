// SatraPortalAI2 — Java ai/portals/SatraPortalAI2.java. Satra dredgion instance portal: initial
// dialog plus auto-group recruiting and a difficulty-confirmation move into the instance.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("satraportal")]
public sealed class SatraPortalAI2 : ActionItemNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened SM_DIALOG_WINDOW(10) (the initial dialog) — not reachable from the script
        // layer. onDialogSelect (auto-group recruit window, group-leader/instance-difficulty
        // confirmation via AI2Request/Player.isInGroup2/PortalService) has no NpcAi2 hook to override,
        // and Player's group2/PortalService.getPortalUse aren't reachable either.
    }
}
