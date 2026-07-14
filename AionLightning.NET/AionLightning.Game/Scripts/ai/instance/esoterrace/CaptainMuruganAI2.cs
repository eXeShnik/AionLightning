// CaptainMuruganAI2 — Java ai/instance/esoterrace/CaptainMuruganAI2.java. Esoterrace boss: once
// aggroed, cycles a periodic self-buff/shout and an enrage burst below 50% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("captain_murugan")]
public sealed class CaptainMuruganAI2 : AggressiveNpcAI2
{
    private bool _isAggred;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isAggred)
        {
            _isAggred = true;
            StartTaskEvent();
        }
    }

    private void StartTaskEvent()
    {
        // note: Java cast skill 19324 lvl10 at the current (player) target here; targeting a creature
        // other than the owner isn't exposed via the UseSkill helper.
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            SendMsg(1500194);
            UseSkill(19325, 5);
            if (Owner.HpPercentage < 50)
            {
                ScheduleTask(() =>
                {
                    if (Owner.IsAlreadyDead) return;
                    SendMsg(1500193);
                    // note: Java cast skill 19324 lvl10 at the current (player) target here too.
                    ScheduleTask(() =>
                    {
                        // note: Java cast skill 19324 lvl10 at the current (player) target here too.
                    }, 4000);
                }, 10000);
            }
        }, 20000, 20000);
    }

    public override void OnBackHome()
    {
        CancelTasks();
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        SendMsg(1500195);
        base.OnDied();
    }
}
