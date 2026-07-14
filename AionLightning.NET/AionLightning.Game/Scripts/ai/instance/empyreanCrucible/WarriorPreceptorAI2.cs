// WarriorPreceptorAI2 — Java ai/instance/empyreanCrucible/WarriorPreceptorAI2.java. Empyrean Crucible
// add: repeating skill event every 30s starting from first attack.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("warrior_preceptor")]
public sealed class WarriorPreceptorAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        // note: Java shouted 1500208 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        base.OnDied();
    }

    public override void OnBackHome()
    {
        CancelTasks();
        _isHome = true;
        base.OnBackHome();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
                CancelTasks();
            else
                StartSkillEvent();
        }, 30000, 30000);
    }

    private void StartSkillEvent()
    {
        // note: Java shouted 1500207 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        // Java also targeted a random living known player within 15m for skill 19595; known-list scanning
        // isn't exposed to scripts yet.
        UseSkill(19595, 10);
        ScheduleTask(() =>
        {
            if (!getOwner().IsAlreadyDead)
                UseSkill(19596, 15);
        }, 6000);
    }
}
