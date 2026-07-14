// PagatiTamerNishakaAI2 — Java ai/instance/rentusBase/PagatiTamerNishakaAI2.java. Rentus Base boss:
// on engage, hides (self-buff) and repeatedly casts a target-aimed skill combo.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pagati_tamer_nishaka")]
public sealed class PagatiTamerNishakaAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isHome) return;
        _isHome = false;
        SendMsg(1500397);
        ScheduleTask(HideEvent, 14000, 14000);
    }

    private void HideEvent()
    {
        if (Owner.IsAlreadyDead) return;
        UseSkill(19660);
        SendMsg(1500398);
        ScheduleTask(() => FireEvent(1500399, 19661), 2000);
        ScheduleTask(() => FireEvent(1500399, 19661), 6000);
        ScheduleTask(() => FireEvent(1500400, 19662), 8000);
    }

    private void FireEvent(int msg, int skill)
    {
        if (Owner.IsAlreadyDead || _isHome) return;
        // note: Java aimed skill 19661 at the owner's current target instead of itself, and only fired
        // when that target was within 5m — target-aware NPC skill casting isn't exposed to scripts yet.
        UseSkill(skill);
        Owner.RemoveEffectBySkillId(19660);
        SendMsg(msg);
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java reopened instance door 98 here — instance doors aren't exposed to scripts yet.
        SendMsg(1500401);
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        Owner.RemoveEffectBySkillId(19660);
        _isHome = true;
        base.OnBackHome();
    }
}
