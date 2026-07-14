using System.Linq;
using System.Collections.Generic;
using System;
// RestoredHetgolemAI2 — Java ai/instance/elementisForest/RestoredHetgolemAI2.java. Wanders briefly
// after spawn, then transforms into its seed form 5s later (or immediately on death).
using AionLightning.Game.Ai;

namespace Ai;

[AiName("restored_hetgolem")]
public sealed class RestoredHetgolemAI2 : AggressiveNpcAI2
{
    private bool _isStartEvent;

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a random-direction wander move via MoveController.moveToPoint after 3s
        // and broadcast an SM_EMOTION(START_EMOTE2); MoveController/PacketSendUtility aren't exposed to
        // scripts yet. Also overrode ask(CAN_RESIST_ABNORMAL=POSITIVE) and pollInstance(SHOULD_DECAY/
        // RESPAWN/REWARD=NEGATIVE) — neither hook exists on NpcAi2.
        StartLifeTask();
    }

    private void StartLifeTask()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
                SpawnEvent();
        }, 5000);
    }

    private void SpawnEvent()
    {
        if (!_isStartEvent)
        {
            _isStartEvent = true;
            Spawn(282308, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
            Spawn(282465, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
            // note: Java deleted the smoke npc (282465) via NpcActions.delete immediately; no scripted
            // despawn API exists yet.
        }
        // note: Java then deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnDied()
    {
        CancelTasks();
        SpawnEvent();
        base.OnDied();
    }
}
