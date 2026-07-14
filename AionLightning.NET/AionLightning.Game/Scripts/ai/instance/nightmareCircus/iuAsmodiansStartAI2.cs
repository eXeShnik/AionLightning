// iuAsmodiansStartAI2 — Java ai/instance/nightmareCircus/iuAsmodiansStartAI2.java. Portal-start npc
// that opens a race-change/portal dialog for Asmodians.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("iu_asmodians_start")]
public sealed class iuAsmodiansStartAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(objectId, 1011) prompt here; dialog packets aren't
        // wired at the script layer yet.
    }

    // note: Java's onDialogSelect(player, dialogId=10000, ...) applied a race-change effect (skill
    // 21332, ~16min) and portalled the player via DataManager.PORTAL2_DATA/PortalService when the
    // dialog choice was accepted, then closed the dialog window. onDialogSelect has no NpcAi2
    // equivalent hook, and SkillEngine.applyEffectDirectly/PortalService aren't wired at the script
    // layer yet.
}
