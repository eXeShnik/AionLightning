// iuElyosStartAI2 — Java ai/instance/nightmareCircus/iuElyosStartAI2.java. Portal-start npc that
// opens a race-change dialog for Elyos.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("iu_elyos_start")]
public sealed class iuElyosStartAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(objectId, 1011) prompt here; dialog packets aren't
        // wired at the script layer yet.
    }

    // note: Java's onDialogSelect(player, dialogId=10000, ...) applied a race-change effect (skill
    // 21329, ~16min) then closed the dialog window. onDialogSelect has no NpcAi2 equivalent hook, and
    // SkillEngine.applyEffectDirectly isn't wired at the script layer yet.
}
