// EmpyreanRecordKeeperAI2 — Java ai/instance/empyreanCrucible/EmpyreanRecordKeeperAI2.java. Per-stage
// dialog NPC that shouts a stage-intro line on spawn and advances the crucible instance on dialog.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("empyreanrecordkeeper")]
public sealed class EmpyreanRecordKeeperAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java shouted a per-npc-id stage-intro message (keyed across ids 799568/799569/205331-
        // 205344) via NpcShoutsService; NPC shouts aren't exposed to scripts yet.
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java opened dialog page 1011 via SM_DIALOG_WINDOW; not exposed to scripts yet. Java's
        // onDialogSelect (dropped — no C# hook exists) advanced the instance through its elevator/round
        // stages via InstanceHandler.onChangeStage keyed by npc id, granted the final instance reward on
        // the last record keeper, and deleted itself via AI2Actions.deleteOwner; instance-handler stage
        // control and scripted despawn aren't exposed to scripts yet.
    }
}
