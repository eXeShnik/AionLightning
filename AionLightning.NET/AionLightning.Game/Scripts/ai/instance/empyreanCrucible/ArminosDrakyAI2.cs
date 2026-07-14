// ArminosDrakyAI2 — Java ai/instance/empyreanCrucible/ArminosDrakyAI2.java. Elevator-corridor drake:
// walks a fixed route twice, then removes itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("arminos_draky")]
public sealed class ArminosDrakyAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java set the spawn template's walker id to "300300001", started WalkManager routing, set
        // the owner's ai2 state to 1, and broadcast a START_EMOTE2 SM_EMOTION packet; walker routing, ai2
        // state, and emote broadcast aren't exposed to scripts yet.
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java tracked MoveController.getCurrentPoint() and, on reaching waypoint 15 for the second
        // time (circle twice), cleared the walker id, stopped WalkManager routing, and deleted itself via
        // AI2Actions.deleteOwner; waypoint tracking and scripted despawn aren't exposed to scripts yet.
    }

    // note: Java also overrode ask (CAN_RESIST_ABNORMAL/SHOULD_REWARD_AP = POSITIVE) and canThink (always
    // false) — neither hook exists on NpcAi2.
}
