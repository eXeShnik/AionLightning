// ArbiterAi2 — Java ai/instance/crucibleChallenge/ArbiterAi2.java. Crucible Challenge dialog NPC:
// opens a confirmation dialog that teleports the player to an npcId-keyed arena location.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

// note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000 —
// teleported the player to one of several hardcoded arena coordinates (map 300320000) keyed by this
// NPC's id via TeleportService2; that hook has no equivalent on NpcAi2, and TeleportService2 isn't
// exposed to scripts yet.
[AiName("arbiter")]
public sealed class ArbiterAi2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(SELECT_ACTION_1011) to open the confirmation dialog;
        // SM_DIALOG_WINDOW and PacketSendUtility aren't ported to the script layer yet.
    }
}
