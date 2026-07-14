// GateRepairAI2 — Java ai/siege/GateRepairAI2.java. Siege gate-repair NPC: prompts the player
// through a two-step confirmation dialog, then starts the gate repair process.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("siege_gaterepair")]
public sealed class GateRepairAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java chained two SM_QUESTION_WINDOW confirmations (repair popup, then repair-stone accept)
        // via RequestResponseHandler before calling OnActivate. RequestResponseHandler/SM_QUESTION_WINDOW
        // aren't exposed to the script layer yet.
    }

    public override void OnDialogFinish(Player player)
    {
    }

    public void OnActivate(Player player)
    {
        // note: Java left this as a stub too ("Start repair process").
    }
}
