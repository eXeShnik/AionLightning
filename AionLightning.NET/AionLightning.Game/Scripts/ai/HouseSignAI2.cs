// HouseSignAI2 — Java ai/HouseSignAI2.java. Opens a DialogPage window selected from the sign's
// dialog-select response.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("housesign")]
public sealed class HouseSignAI2 : GeneralNpcAI2
{
    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) resolved a DialogPage
    // and opened it via SM_DIALOG_WINDOW. That dialog-select hook, DialogPage, and SM_DIALOG_WINDOW aren't
    // ported to the script layer yet.
}
