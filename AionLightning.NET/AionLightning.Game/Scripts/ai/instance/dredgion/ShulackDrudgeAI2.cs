// ShulackDrudgeAI2 — Java ai/instance/dredgion/ShulackDrudgeAI2.java. Dredgion Shulack Drudge:
// opens a dialog window and grants a race-specific supply item once per player.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("shulackdrudge")]
public sealed class ShulackDrudgeAI2 : GeneralNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(objectId, 1011) packet here; dialog-window packets aren't
        // wired at the script layer yet.
    }

    public override void OnDialogFinish(Player player)
    {
        base.OnDialogFinish(player);
        // note: Java granted a race-specific "dredgion supplies" item (once per player) via ItemService,
        // then switched its npc-type to peace and broadcast an SM_CUSTOM_SETTINGS update to known players;
        // item granting and custom-settings broadcasts aren't wired at the script layer yet.
    }
}
