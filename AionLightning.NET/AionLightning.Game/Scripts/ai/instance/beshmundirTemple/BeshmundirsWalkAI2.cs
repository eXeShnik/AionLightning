// BeshmundirsWalkAI2 — Java ai/instance/beshmundirTemple/BeshmundirsWalkAI2.java. Instance-entry NPC:
// difficulty/path selection dialog that ports the requesting group into Beshmundir's Walk.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("beshmundirswalk")]
public sealed class BeshmundirsWalkAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java opened the initial instance-entry dialog (page 10) here, then handled group-finder,
        // path-selection, and difficulty-confirmation prompts and the actual PortalService.port call in
        // onDialogSelect — that dialog-select hook has no C# equivalent. SM_DIALOG_WINDOW, SM_FIND_GROUP,
        // AutoGroupType, AI2Request confirmation, and PortalService aren't wired at the script layer yet.
    }
}
