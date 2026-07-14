// VershakaStatueAI2 — Java ai/instance/tiamatStrongHold/VershakaStatueAI2.java. Statue: offers a
// dialog that grants a timed buff to the player.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("vershakastatue")]
public sealed class VershakaStatueAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(1011) packet to the player. PacketSendUtility and
        // SM_DIALOG_WINDOW aren't exposed to scripts yet.
    }

    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000
    // applied a 3-minute buff (skill 2784) directly to the player via SkillEngine.applyEffectDirectly.
    // onDialogSelect has no equivalent hook on NpcAi2, and direct-effect application isn't exposed to
    // scripts yet.
}
