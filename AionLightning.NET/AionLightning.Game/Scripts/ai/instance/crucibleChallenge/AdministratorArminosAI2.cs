// AdministratorArminosAI2 — Java ai/instance/crucibleChallenge/AdministratorArminosAI2.java.
// Crucible Challenge dialog NPC: opens a confirmation dialog that spawns a zone-specific monster
// trio and deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

// note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000 —
// checked which illusion-stadium zone the player stood in and spawned a matching monster trio before
// deleting itself via AI2Actions.deleteOwner; that hook has no equivalent on NpcAi2, and ZoneName
// membership checks aren't exposed to scripts yet.
[AiName("administratorarminos")]
public sealed class AdministratorArminosAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(SELECT_ACTION_1011) to open the confirmation dialog;
        // SM_DIALOG_WINDOW and PacketSendUtility aren't ported to the script layer yet.
    }
}
