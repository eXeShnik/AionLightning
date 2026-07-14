// InvincibleShabokanAI2 — Java ai/instance/tiamatStrongHold/InvincibleShabokanAI2.java. Tiamat
// Stronghold boss: alternates an earthquake/sink hazard event on a timer, plus a 25%-HP self-buff.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("invincibleshabokan")]
public sealed class InvincibleShabokanAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private bool _isFinalBuff;

    public override void OnAttack(Creature attacker)
    {
        base.OnAttack(attacker);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
        if (!_isFinalBuff && Owner.HpPercentage <= 25)
        {
            _isFinalBuff = true;
            UseSkill(20941);
        }
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) CancelTasks();
            else ChooseRandomEvent();
        }, 5000, 30000);
    }

    private void EarthQuakeEvent()
    {
        UseSkill(20717);
        if (GetNpc(283082) is null)
        {
            Spawn(283082, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        }
    }

    private void SinkEvent()
    {
        UseSkill(20720);
        // note: Java spawned traps 283083/283084 under every known player within 30m of the boss —
        // known-player enumeration isn't exposed to scripts yet.
    }

    private void ChooseRandomEvent()
    {
        if (System.Random.Shared.Next(2) == 0) EarthQuakeEvent();
        else SinkEvent();
    }

    public override void OnDied()
    {
        base.OnDied();
        CancelTasks();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        CancelTasks();
        Owner.RemoveEffectBySkillId(20941);
        _isHome = true;
    }
}
