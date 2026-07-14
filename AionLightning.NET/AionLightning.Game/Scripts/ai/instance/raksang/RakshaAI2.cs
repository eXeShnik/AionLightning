// RakshaAI2 — Java ai/instance/raksang/RakshaAI2.java. Raksang boss: intro shout on first attack, a
// 75%-HP shout that starts a repeating rubble-hazard spawn cycle, and a rubble-guard add on death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("raksha")]
public sealed class RakshaAI2 : AggressiveNpcAI2
{
    private bool _isAggred;
    private bool _isStartedEvent;

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        Spawn(730445, 1062.281f, 889.900f, 138.744f, 29);
        base.OnDied();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isAggred)
        {
            _isAggred = true;
            // note: Java shouted 1401152 via NpcShoutsService here; NPC shouts aren't exposed to scripts
            // yet.
        }
        CheckPercentage(getOwner().HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 75 || _isStartedEvent) return;
        _isStartedEvent = true;
        // note: Java shouted 1401154 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        StartPhaseTask();
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
            UseSkill(19938, 46);
            // note: Java then picked 3..knownPlayerCount (or all, if fewer than 4) living known players
            // and spawned a raksang_rubble (282325) at each one's position 3s later; known-list iteration
            // isn't exposed to scripts yet.
        }, 3000, 15000);
    }

    public override void OnBackHome()
    {
        CancelTasks();
        _isStartedEvent = false;
        _isAggred = false;
        base.OnBackHome();
    }
}
