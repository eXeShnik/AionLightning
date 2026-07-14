// DaevaDayAI2 — Java ai/events/DaevaDayAI2.java. Daeva Day event NPC: only shows its standard talk
// dialog for the two named variants, defaulting to a plain dialog window otherwise.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("daeva_day_new")]
public sealed class DaevaDayAI2 : GeneralNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        switch (Owner.Template.NpcId)
        {
            case 831921:
            case 831922:
                base.OnDialogStart(player);
                break;
            default:
                // note: Java sent SM_DIALOG_WINDOW(1011) — not reachable from the script layer.
                break;
        }
    }

    // note: Java's onDialogSelect cast a random buff skill (10825 or 10826) via SkillEngine on SETPRO1
    // and forwarded QUEST_SELECT dialogs through QuestEngine/SM_DIALOG_WINDOW. onDialogSelect has no
    // NpcAi2 hook to override, and QuestEngine/SkillEngine aren't reachable from the script layer.
}
