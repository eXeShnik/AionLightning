// ShugoAdmirerAI2 — Java ai/instance/shugoImperialTomb/ShugoAdmirerAI2.java. Shugo Imperial Tomb
// stage-start NPC: opens a dialog and, on selection, advances the instance's stage list then deletes
// itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("shugoadmirer")]
// 831110, 831111, 831112
public sealed class ShugoAdmirerAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(1011); PacketSendUtility isn't exposed to scripts yet.
    }

    // note: Java's onDialogSelect (no C# equivalent hook) advanced the instance's stage list
    // (InstanceHandler.onChangeStageList — START_STAGE_1/2/3_PHASE_1, chosen by this NPC's id), closed
    // the dialog, then deleted itself (AI2Actions.deleteOwner). InstanceHandler/StageList access and
    // NPC self-deletion aren't exposed to scripts yet.
}
