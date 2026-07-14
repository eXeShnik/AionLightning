// GoldenEyeMantutuAI2 — Java ai/instance/steelRake/GoldenEyeMantutuAI2.java. Steel Rake mantutu
// boss: periodically casts a hunger/thirst debuff on itself once engaged, and clears an escort npc's
// effect on death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("golden_eye_mantutu")]
public sealed class GoldenEyeMantutuAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    // note: Java's canThink()/handleCustomEvent(int,Object...)/ask(AIQuestion) overrides plumbed a
    // "walk to the feed/water supply device and stand still while feeding" flow through ai2-framework
    // internals (custom events, AIState, MoveController, EmoteManager) with no NpcAi2 equivalent —
    // dropped entirely, along with the OnMoveArrived feed-approach branch that only fired from that
    // custom event.
    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            DoSchedule();
        }
    }

    public override void OnDespawned()
    {
        CancelTasks(); // note: Java only cancelled its hunger task; this AI schedules nothing else.
        base.OnDespawned();
    }

    public override void OnDied()
    {
        // note: Java also cleared abnormal effect 18189 from instance npc 219037 here; cross-npc
        // effect lookup by id isn't wired at the script layer yet.
        base.OnDied(); // cancels the scheduled hunger task
    }

    public override void OnBackHome()
    {
        CancelTasks();
        Owner.RemoveEffectBySkillId(20489);
        Owner.RemoveEffectBySkillId(20490);
        _isHome = true;
        base.OnBackHome();
    }

    private void DoSchedule()
    {
        ScheduleTask(() =>
        {
            int skill = System.Random.Shared.Next(1, 3) == 1 ? 20489 : 20490; // Hunger / Thirst
            UseSkill(skill, 20);
        }, 10000, 30000);
    }
}
