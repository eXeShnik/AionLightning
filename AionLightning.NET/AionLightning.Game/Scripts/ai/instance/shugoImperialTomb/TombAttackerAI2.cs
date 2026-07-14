// TombAttackerAI2 — Java ai/instance/shugoImperialTomb/TombAttackerAI2.java. Shugo Imperial Tomb
// walker NPC: once its walk route finishes, adds hate toward the tomb's defense towers.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("tombattacker")]
// 219508
public sealed class TombAttackerAI2 : AggressiveNpcAI2
{
    // note: Java also overrode modifyOwnerDamage (clamp to 1) and canThink (locked out until its walk
    // route finished) — neither hook exists on NpcAi2 yet.

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java checked whether this was the walker route's final point (DataManager.WALKER_DATA +
        // getMoveController().getCurrentPoint()) and, once there, stopped walking (WalkManager),
        // re-enabled thinking, and added hate toward the tomb's towers (831251/831250/831304/831305/
        // 831130) via EmoteManager.emoteStopAttacking + AggroList.addHate. Walker-route/move-controller
        // state and EmoteManager aren't exposed to scripts yet.
    }
}
