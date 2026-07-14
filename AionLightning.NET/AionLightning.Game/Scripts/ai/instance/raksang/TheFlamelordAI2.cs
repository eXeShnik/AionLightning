// TheFlamelordAI2 — Java ai/instance/raksang/TheFlamelordAI2.java. Raksang boss: HP-threshold
// escalation (90%/40%/30%/20%/10%) that moves an increasing number of executor adds onto their
// target props.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("the_flamelord")]
public sealed class TheFlamelordAI2 : AggressiveNpcAI2
{
    private bool _isAggred;
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 90:
                    StartPhaseTask();
                    break;
                case 40:
                case 30:
                case 20:
                case 10:
                    StartPhaseEvent(percent);
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void StartPhaseEvent(int percent)
    {
        CancelTasks();
        // note: Java shouted 1401120 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        UseSkill(19980, 46);
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead) return;
            switch (percent)
            {
                case 40:
                    MoveExecutor(282451);
                    break;
                case 30:
                    MoveExecutor(282451);
                    MoveExecutor(282452);
                    break;
                case 20:
                    MoveExecutor(282451);
                    MoveExecutor(282452);
                    MoveExecutor(282453);
                    break;
                case 10:
                    MoveExecutor(282451);
                    MoveExecutor(282452);
                    MoveExecutor(282453);
                    MoveExecutor(282454);
                    break;
            }
            UseSkill(19924, 44);
            CancelTasks();
            StartPhaseTask();
        }, 5000);
    }

    private void MoveExecutor(int executorId)
    {
        var npc = Spawn(executorId, 802.845f, 964.903f, 792.102f, 0);
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead || npc is null) return;
            int targetId = executorId switch
            {
                282451 => 701062,
                282452 => 701063,
                282453 => 701064,
                282454 => 701065,
                _ => 0,
            };
            var target = targetId != 0 ? GetNpc(targetId) : null;
            if (target is not null)
            {
                npc.Target = target;
                // note: Java issued a moveToTargetObject() move order here; move-controller access isn't
                // exposed to scripts yet.
            }
        }, 1500);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 90, 40, 30, 20, 10 });
    }

    private void StartPhaseTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            UseSkill(19925, 44);
            // note: Java shouted 1401119 via NpcShoutsService here; NPC shouts aren't exposed to scripts
            // yet.
        }, 3000, 30000);
    }

    public override void OnDied()
    {
        _percents.Clear();
        CancelTasks();
        // note: Java opened instance door 118 here; door control isn't exposed to scripts yet. Also
        // shouted 1401121 via NpcShoutsService.
        base.OnDied();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnAttack(Creature creature)
    {
        if (!_isAggred)
        {
            _isAggred = true;
            // note: Java shouted 1401118 via NpcShoutsService here; NPC shouts aren't exposed to scripts
            // yet.
        }
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTasks();
        _isAggred = false;
        base.OnBackHome();
    }
}
