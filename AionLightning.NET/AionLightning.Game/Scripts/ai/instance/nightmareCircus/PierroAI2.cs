// PierroAI2 — Java ai/instance/nightmareCircus/PierroAI2.java. Event npc that opens a race-specific
// effect dialog.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("event_pierro")]
public sealed class PierroAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(objectId, 1011) prompt here; dialog packets aren't
        // wired at the script layer yet.
    }

    // note: Java's onDialogSelect(player, dialogId=10000, ...) applied skill 21334 (Asmodian) or
    // 21331 (Elyos/other) directly to the player, then closed the dialog window. onDialogSelect has
    // no NpcAi2 equivalent hook, and SkillEngine.applyEffectDirectly isn't wired at the script layer
    // yet.
}
