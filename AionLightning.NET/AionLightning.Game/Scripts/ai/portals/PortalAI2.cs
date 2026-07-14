// PortalAI2 — Java ai/portals/PortalAI2.java. Root base for portal NPCs: resolves the destination
// portal path/teleport template on spawn, then teleports the player on dialog/use-item completion.
// note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) (always returned true)
// has no NpcAi2 hook to override.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("portal")]
public class PortalAI2 : ActionItemNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java's AI2Actions.selectDialog(this, player, 0, -1) has no C# equivalent — dropped.
        if (GetTalkDelay() != 0)
            base.OnDialogStart(player);
        else
            HandleUseItemFinish(player);
    }

    /// <summary>Java <c>handleUseItemFinish</c>: resolved a <c>PortalUse</c>/<c>TeleporterTemplate</c>
    /// via <c>DataManager.PORTAL2_DATA</c>/<c>TELEPORTER_DATA</c> and teleported the player through
    /// <c>PortalService.port</c>/<c>TeleportService2.teleport</c>.</summary>
    protected override void HandleUseItemFinish(Player player)
    {
        // note: portal teleport is now handled directly by CM_SHOW_DIALOG via the injected
        // PortalService before dialog reaches AI scripts (see Network/Aion/ClientPackets/CM_SHOW_DIALOG.cs),
        // so this hook is effectively dead code in the ported flow — kept as a no-op for structural parity.
    }
}
