// PostboxAI2 — Java ai/PostboxAI2.java. Opens the player's mailbox dialog on talk.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("postbox")]
public sealed class PostboxAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened the mailbox dialog (SM_DIALOG_WINDOW + Mailbox.sendMailList(false));
        // Player.Mailbox and SM_DIALOG_WINDOW aren't ported to the script layer yet.
    }

    public override void OnDialogFinish(Player player)
    {
    }
}
