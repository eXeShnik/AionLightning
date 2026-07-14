// RecordkeeperAI2 — Java ai/instance/crucibleChallenge/RecordkeeperAI2.java. Crucible Challenge
// stage-progression NPC: a dialog-driven state machine that advances the instance through its six
// stages, teleporting the player and spawning the next stage's NPCs at each step.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

// note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000 — drove
// the entire stage-progression state machine (InstanceHandler.onChangeStage, TeleportService2, per-stage
// npc spawns, and a random stage-4 branch), plus a final reward hand-off for npcId 205679, before
// deleting itself via AI2Actions.deleteOwner. That hook has no equivalent on NpcAi2, and
// InstanceHandler/StageType/TeleportService2 aren't exposed to scripts yet.
[AiName("recordkeeper")]
public sealed class RecordkeeperAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(SELECT_ACTION_1011) to open the stage dialog; SM_DIALOG_WINDOW
        // and PacketSendUtility aren't ported to the script layer yet.
    }
}
