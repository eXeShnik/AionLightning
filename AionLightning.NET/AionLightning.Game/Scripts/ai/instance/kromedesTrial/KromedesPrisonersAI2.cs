// KromedesPrisonersAI2 — Java ai/instance/kromedesTrial/KromedesPrisonersAI2.java. Dialog NPC that
// self-deletes on a specific dialog selection.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("krprisoners")]
public sealed class KromedesPrisonersAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(objectId, 1011) here, and (via the dropped onDialogSelect
        // hook) on dialogId 10000 deleted the owner (AI2Actions.deleteOwner) or on dialogId 1012
        // reopened page 1012. PacketSendUtility/SM_DIALOG_WINDOW, AI2Actions, and the dialog-select
        // hook aren't exposed to the script layer yet.
    }
}
