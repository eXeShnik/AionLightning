// KromedesMagasAI2 — Java ai/instance/kromedesTrial/KromedesMagasAI2.java. Dialog NPC that plays a
// cutscene when the player carries a specific item.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("krmagas")]
public sealed class KromedesMagasAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(objectId, 1011) here, and (via the dropped onDialogSelect
        // hook) on dialogId 10000 played movie 454 + reopened the dialog if the player carried item
        // 185000109 (else showed page 27), or on dialogId 1012 reopened page 1012. PacketSendUtility/
        // SM_DIALOG_WINDOW/SM_PLAY_MOVIE and the dialog-select hook aren't exposed to the script layer
        // yet.
    }
}
