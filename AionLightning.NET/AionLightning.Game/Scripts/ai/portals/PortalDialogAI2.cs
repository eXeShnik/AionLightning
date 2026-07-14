// PortalDialogAI2 — Java ai/portals/PortalDialogAI2.java. Portal NPC that shows a quest-state-aware
// dialog (reward/quest/starting/teleportation) instead of teleporting immediately.
// note: Java's onDialogSelect forwarded to QuestEngine.onDialog, handled auto-group recruit dialogs
// via AutoGroupType/SM_AUTO_GROUP/SM_FIND_GROUP, and otherwise resolved a PortalPath by dialogId via
// DataManager.PORTAL2_DATA and called PortalService.port. onDialogSelect has no NpcAi2 hook to
// override, and PortalService/SM_DIALOG_WINDOW aren't reachable from the script layer for this flow.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("portal_dialog")]
public class PortalDialogAI2 : PortalAI2
{
    protected int teleportationDialogId = 1011;
    protected int rewardDialogId = 5;
    protected int startingDialogId = 10;
    protected int questDialogId = 10;

    public override void OnDialogStart(Player player)
    {
        if (GetTalkDelay() == 0)
            CheckDialog(player);
        else
            base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player) => CheckDialog(player);

    /// <summary>Java <c>checkDialog</c>: picked between the reward/quest/starting/teleportation
    /// SM_DIALOG_WINDOW (with a few npcId-specific dialog overrides) based on the player's related-quest
    /// state via QuestEngine/QuestService.</summary>
    private void CheckDialog(Player player)
    {
        // note: QuestEngine, QuestService and SM_DIALOG_WINDOW aren't reachable from the script layer.
    }
}
