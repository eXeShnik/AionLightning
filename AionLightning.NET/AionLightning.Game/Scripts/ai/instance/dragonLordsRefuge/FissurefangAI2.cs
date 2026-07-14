// FissurefangAI2 — Java ai/instance/dragonLordsRefuge/FissurefangAI2.java. Dragon Lords' Refuge boss:
// periodic sinking-sand/cavity event, plus an enrage skill once either empyrean god has died.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("fissurefang")]
// 219365
public sealed class FissurefangAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
        IsDeadGod();
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
                CancelTasks();
            else
                SinkEvent();
        }, 5000, 30000);
    }

    private void SinkEvent()
    {
        int npc = Random.Shared.Next(0, 2) == 0 ? 282735 : 282737;
        int skill = npc == 282735 ? 20718 : 20172;
        if (skill == 20172)
        {
            UseSkill(20476);
        }
        UseSkill(skill);
        // note: Java then spawned `npc` at every known player's position within 30m of itself; known-list
        // iteration and range checks aren't exposed to scripts yet.
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        CancelTasks();
        _isHome = true;
    }

    private bool IsDeadGod()
    {
        var marcutan = GetNpc(219491);
        var kaisinel = GetNpc(219488);
        if (IsDead(marcutan) || IsDead(kaisinel))
        {
            UseSkill(20983);
            return true;
        }
        return false;
    }

    private static bool IsDead(Npc? npc) => npc is not null && npc.IsAlreadyDead;
}
