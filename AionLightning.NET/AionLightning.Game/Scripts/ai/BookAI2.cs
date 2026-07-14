// BookAI2 — Java ai/BookAI2.java. Opens a fixed dialog page when talked to.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("book")]
public sealed class BookAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(SELECT_ACTION_1011) to open the book UI; SM_DIALOG_WINDOW and
        // PacketSendUtility aren't ported to the script layer yet.
    }
}
