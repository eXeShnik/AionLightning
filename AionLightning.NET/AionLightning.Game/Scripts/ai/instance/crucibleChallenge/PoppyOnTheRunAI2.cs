// PoppyOnTheRunAI2 — Java ai/instance/crucibleChallenge/PoppyOnTheRunAI2.java. Scripted event NPC
// that plays an emote on spawn and never thinks.
using AionLightning.Game.Ai;

namespace Ai;

// note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) just closed the dialog
// window (SM_DIALOG_WINDOW); that hook has no equivalent on NpcAi2 and PacketSendUtility isn't exposed
// to scripts yet.
[AiName("poppyontherun")]
public sealed class PoppyOnTheRunAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java set a custom NPC state and broadcast an SM_EMOTION(START_EMOTE2) packet; NPC state
        // and SM_EMOTION broadcasting aren't exposed to scripts yet (also overrode canThink() to return
        // false — no C# equivalent hook exists).
    }
}
