// ResurrectAI2 — Java ai/ResurrectAI2.java. Bind-point (resurrection) NPC: validates race/world
// rules then runs a paid bind-point registration confirmation.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("resurrect")]
public sealed class ResurrectAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java looked up a BindPointTemplate, validated race/cross-faction/world-type rules
        // (skipping prison worlds and already-registered points), then ran an AI2Request confirmation
        // (kinah cost, distance check) before storing the new bind point via PlayerBindPointDAO and
        // calling TeleportService2.sendSetBindPoint. BindPointTemplate/PlayerBindPointDAO/AI2Request
        // aren't wired at the script layer yet.
    }
}
