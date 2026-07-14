// WorldBlesserAI2 — Java ai/worlds/WorldBlesserAI2.java. Opens a normal quest dialog for named blesser
// npcs; every other npc-id opens a raw dialog window instead.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("world_blesser")]
public sealed class WorldBlesserAI2 : GeneralNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        switch (Owner.Template.NpcId)
        {
            case 831024: // Renniah
            case 831027: // Karzanke
            case 831028: // Erdat
            case 831029: // Edandos
            case 831030: // Netalion
            case 831031: // Nebrith
                base.OnDialogStart(player);
                break;
            default:
                // note: Java opened SM_DIALOG_WINDOW(1011) here; dialog packets aren't exposed to scripts
                // yet.
                break;
        }
        // note: Java's onDialogSelect (dialogId 10000 -> cast blessing skill 20950; questId != 0 ->
        // forward to QuestEngine.onDialog / reopen the dialog window) has no matching NpcAi2 hook and
        // isn't ported.
    }
}
