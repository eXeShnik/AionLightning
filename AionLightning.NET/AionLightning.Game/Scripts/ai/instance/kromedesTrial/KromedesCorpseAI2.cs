// KromedesCorpseAI2 — Java ai/instance/kromedesTrial/KromedesCorpseAI2.java. Dialog NPC that
// grants a quest item once on first talk-through.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("krcorpse")]
public sealed class KromedesCorpseAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(objectId, 1011) here, and (via the dropped onDialogSelect
        // hook) on dialogId 1012 granted item 164000141 the first time (SM_DIALOG_WINDOW 1012 +
        // SM_SYSTEM_MESSAGE 1400701 + ItemService.addItem) or re-showed page 27 once already granted.
        // PacketSendUtility/SM_DIALOG_WINDOW, ItemService, and the dialog-select hook aren't exposed
        // to the script layer yet.
    }
}
