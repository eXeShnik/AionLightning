// PradethMercenaryElyosAI2 — Java ai/siege/pradeth/PradethMercenaryElyosAI2.java. Pradeth
// reinforcement NPC (Elyos side): gates its dialog on holding a Blood Mark, then spawns three waves
// of mercenary defenders per dialog-select stage.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pradeth_mercenary_elyos")]
public sealed class PradethMercenaryElyosAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java allowed the default dialog only when the player held item 186000236 (Blood Mark),
        // otherwise closed the window with a "need item" message. Item-count checks and dialog-window
        // packets aren't exposed to the script layer yet (also overrode onDialogSelect to spawn three
        // waves of mercenary NPCs per SETPRO1/2/3 stage — no dialog-select hook exists in NpcAi2).
    }
}
