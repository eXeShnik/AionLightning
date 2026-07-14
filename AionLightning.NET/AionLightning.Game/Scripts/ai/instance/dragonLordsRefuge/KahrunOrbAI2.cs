// KahrunOrbAI2 — Java ai/instance/dragonLordsRefuge/KahrunOrbAI2.java. Dialog orb that opens the
// instance portal once a player confirms the prompt.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kahrunOrb")]
// 800429
public sealed class KahrunOrbAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened SM_DIALOG_WINDOW page 1011 unless the portal NPC (730625) was already spawned;
        // SM_DIALOG_WINDOW isn't wired to the script layer yet (also overrode onDialogSelect to spawn the
        // portal on dialog 10000 and close the window — that dialog-select hook and SM_DIALOG_WINDOW
        // aren't ported either).
    }
}
