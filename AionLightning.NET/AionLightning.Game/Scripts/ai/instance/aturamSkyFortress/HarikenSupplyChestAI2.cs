// HarikenSupplyChestAI2 — Java ai/instance/aturamSkyFortress/HarikenSupplyChestAI2.java. Reward
// chest: opens a dialog window and grants a one-time item pair on selection.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("hariken_supply_chest")]
public sealed class HarikenSupplyChestAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        base.OnDialogStart(player);
        // note: Java sent an SM_DIALOG_WINDOW(1011) packet to the player. PacketSendUtility and
        // SM_DIALOG_WINDOW aren't exposed to scripts yet.
    }

    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000
    // granted a "Talon Summoning Device" (164000163) and "Bottomless Bucket" (164000202) once, via
    // ItemService.addItem, then closed the dialog with SM_DIALOG_WINDOW(0) — has no equivalent hook on
    // NpcAi2, and ItemService/Inventory item lookup aren't exposed to scripts yet.
}
