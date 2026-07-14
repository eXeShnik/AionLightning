// PlegetonAI2 — Java ai/instance/beshmundirTemple/PlegetonAI2.java. Path-selection NPC that plays a
// cinematic and teleports the player to the chosen branch of the instance.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("plegeton")]
public sealed class PlegetonAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened dialog page 1011 here; the real per-npcId branching (SM_PLAY_MOVIE playback,
        // TeleportService2 teleport to the chosen branch, a 420s instance-door timer, and an
        // all-players quest-timer broadcast) lived in onDialogSelect, which has no C# equivalent hook.
        // SM_DIALOG_WINDOW/SM_PLAY_MOVIE/TeleportService2/instance doors/instance-wide broadcast aren't
        // wired at the script layer yet either.
    }
}
