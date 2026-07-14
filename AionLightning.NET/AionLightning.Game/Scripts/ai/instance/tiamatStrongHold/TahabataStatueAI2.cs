// TahabataStatueAI2 — Java ai/instance/tiamatStrongHold/TahabataStatueAI2.java. Statue: on
// use-item, offers a dialog that teleports the player out of the instance.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tahabatastatue")]
public sealed class TahabataStatueAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(SELECT_ACTION_1352) packet here. PacketSendUtility and
        // SM_DIALOG_WINDOW aren't exposed to scripts yet.
    }

    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000
    // teleported the player to map 300510000 via TeleportService2. onDialogSelect has no equivalent
    // hook on NpcAi2, and TeleportService2 isn't exposed to scripts yet.
}
