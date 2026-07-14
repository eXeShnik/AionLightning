using System.Linq;
using System.Collections.Generic;
using System;
// UnstableGatesSummonedAI2 — Java ai/instance/unstableSplinterpath/UnstableGatesSummonedAI2.java.
// Summoned add that walks to the boss npc on spawn, then alternates between two skills on it every 30s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("summonedunstablelapilima")]
public sealed class UnstableGatesSummonedAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java disabled its own think loop (canThink, no C# equivalent), stopped its attack emote,
        // set state FOLLOWING, targeted the boss npc (219563) via AI2Actions.targetCreature, and moved to
        // it. EmoteManager, AI2Actions targeting and MoveController.moveToTargetObject aren't exposed to
        // scripts yet, so OnMoveArrived below never fires from this path.
    }

    public override void OnDied()
    {
        base.OnDied(); // CancelTasks() (from NpcAi2.OnDied) covers Java's explicit event-task cancel.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead)
                CancelTasks();
            else
                UseSkill(Random.Shared.Next(2) == 0 ? 19257 : 19281);
        }, 5000, 30000);
    }
}
