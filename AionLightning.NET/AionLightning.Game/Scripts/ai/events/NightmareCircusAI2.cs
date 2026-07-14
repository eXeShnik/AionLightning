// NightmareCircusAI2 — Java ai/events/NightmareCircusAI2.java. Nightmare Circus instance entrance:
// opens a dialog and offers auto-group recruiting.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("nightmare_circus")]
public sealed class NightmareCircusAI2 : ActionItemNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened SM_DIALOG_WINDOW(10) — not reachable from the script layer. onDialogSelect
        // (auto-group recruit window via AutoGroupType/SM_FIND_GROUP on dialogId 105) has no NpcAi2
        // hook to override either.
    }
}
