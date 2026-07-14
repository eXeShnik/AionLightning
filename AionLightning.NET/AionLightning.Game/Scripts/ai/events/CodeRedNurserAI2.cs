// CodeRedNurserAI2 — Java ai/events/CodeRedNurserAI2.java. Code Red Nurse event NPC: gates its
// standard talk/spawn behaviour to specific weekday windows per nurse variant.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("code_red_nurse")]
public sealed class CodeRedNurserAI2 : GeneralNpcAI2
{
    private static readonly int[] MonThuNpcs = { 831435, 831436, 831441, 831442 };
    private static readonly int[] FriSatNpcs = { 831437, 831518, 831443, 831524 };

    public override void OnDialogStart(Player player)
    {
        switch (Owner.Template.NpcId)
        {
            case 831435: case 831436: case 831437: case 831518:
            case 831441: case 831442: case 831443: case 831029:
            case 831031: case 831524:
                base.OnDialogStart(player);
                break;
            default:
                // note: Java sent SM_DIALOG_WINDOW(1011) — not reachable from the script layer.
                break;
        }
    }

    // note: Java's onDialogSelect resolved SETPRO1 into a per-nurse buff pair (removed one skill via
    // Player.getEffectController().removeEffect, cast the other via SkillEngine) and forwarded
    // QUEST_SELECT dialogs to QuestEngine/SM_DIALOG_WINDOW. onDialogSelect has no NpcAi2 hook to
    // override, and QuestEngine/effect-removal-by-skill-id/SkillEngine aren't reachable from the script
    // layer.

    public override void OnSpawned()
    {
        int isoDay = IsoDayOfWeek(DateTime.Now);
        int npcId = Owner.Template.NpcId;
        if (Array.IndexOf(MonThuNpcs, npcId) >= 0)
        {
            if (isoDay is >= 1 and <= 4) base.OnSpawned();
            // note: Java despawned itself (getController().onDelete()) outside the window when not
            // already dead — NPC self-despawn isn't exposed to the script layer.
        }
        else if (Array.IndexOf(FriSatNpcs, npcId) >= 0)
        {
            if (isoDay is >= 5 and <= 7) base.OnSpawned();
        }
    }

    /// <summary>Joda-time <c>DateTime.getDayOfWeek()</c> numbering: Monday=1 .. Sunday=7.</summary>
    private static int IsoDayOfWeek(DateTime now) => now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
}
