// BigOrbAI2 — Java ai/instance/beshmundirTemple/BigOrbAI2.java. Dialog orb that spawns the instance
// portal once a player confirms the prompt.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("bigorb")]
public sealed class BigOrbAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened dialog page 1011, or page 10 when the portal (730276) was already spawned
        // (getWorldMapInstance().getNpcs), then spawned the portal on dialog-select (SETPRO1) — that
        // dialog-select hook has no C# equivalent, and SM_DIALOG_WINDOW isn't wired at the script layer yet.
    }
}
