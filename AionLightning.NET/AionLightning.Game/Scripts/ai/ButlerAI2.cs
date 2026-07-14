// ButlerAI2 — Java ai/ButlerAI2.java. House butler NPC: streams the house owner's script pages to
// a visiting player and answers house-dialog page selections.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("butler")]
public sealed class ButlerAI2 : GeneralNpcAI2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java streamed the house owner's PlayerScript byte buffers to the visiting player via
        // SM_HOUSE_SCRIPTS in <=8141-byte chunks. House.PlayerScripts/SM_HOUSE_SCRIPTS aren't ported to
        // the script layer yet (also overrode onDialogSelect to open a DialogPage — that dialog-select
        // hook and DialogPage/SM_DIALOG_WINDOW aren't ported either).
    }
}
