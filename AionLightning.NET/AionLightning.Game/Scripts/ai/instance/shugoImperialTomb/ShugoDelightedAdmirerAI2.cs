// ShugoDelightedAdmirerAI2 — Java ai/instance/shugoImperialTomb/ShugoDelightedAdmirerAI2.java. Shugo
// Imperial Tomb NPC: grants a race-dependent buff then teleports the player to a fixed point.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("shugodelightedadmirer")]
// 831114, 831306, 831115, 831195
public sealed class ShugoDelightedAdmirerAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(1011); PacketSendUtility isn't exposed to scripts yet.
    }

    // note: Java's onDialogSelect (no C# equivalent hook) handled two DialogAction cases: SETPRO2
    // applied a race-dependent buff effect (SkillEngine.applyEffectDirectly, chosen by this NPC's id)
    // and opened dialog 1012; SETPRO1 closed the dialog and teleported the player to one of two fixed
    // points (TeleportService2, chosen by this NPC's id). Effect application, TeleportService2, and
    // PacketSendUtility aren't exposed to scripts yet.
}
