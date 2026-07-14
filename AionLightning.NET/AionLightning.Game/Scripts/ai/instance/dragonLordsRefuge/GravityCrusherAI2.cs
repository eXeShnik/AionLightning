// GravityCrusherAI2 — Java ai/instance/dragonLordsRefuge/GravityCrusherAI2.java. Tiamat add: charges
// a nearby player, casts a repeating skill on arrival, then self-destructs into a gravity tornado.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("gravitycrusher")]
// 283141, 283142
public sealed class GravityCrusherAI2 : AggressiveNpcAI2
{
    // note: Java also overrode canThink to return false and pollInstance to refuse decay/respawn/reward —
    // neither has a C# equivalent.

    public override void OnSpawned()
    {
        base.OnSpawned();
        Transform();
        ScheduleTask(AttackPlayer, 2000);
    }

    private void Transform()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead) return;
            UseSkill(20967); // self destruct
            ScheduleTask(() =>
            {
                // note: Java shouted (1401554) before spawning the gravity-tornado add at this NPC's
                // position and deleting itself; NPC shouts and scripted self-delete aren't exposed to
                // scripts yet.
                Spawn(283140, getOwner().Position.X, getOwner().Position.Y, getOwner().Position.Z, (byte)getOwner().Position.Heading);
            }, 3000);
        }, 30000);
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        ScheduleTask(() => UseSkill(20987), 0, 4000);
    }

    private void AttackPlayer()
    {
        // note: Java picked a random living known-player within 200m, set it as target, switched to
        // AiState.WALKING, issued a move-to-target order, and broadcast a START_EMOTE2 packet. Known-list
        // iteration, target assignment, and move-controller/emote-packet access aren't exposed to scripts
        // yet.
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
