// BrigadeGeneralChantraAI2 — Java ai/instance/tiamatStrongHold/BrigadeGeneralChantraAI2.java.
// Tiamat Stronghold boss: periodically spawns a random trap ring, and self-buffs at 25% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("brigadegeneralchantra")]
public sealed class BrigadeGeneralChantraAI2 : AggressiveNpcAI2
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
            UseSkill(20942);
        }
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) CancelTasks();
            else StartTrapEvent();
        }, 5000, 40000);
    }

    private void StartTrapEvent()
    {
        int[] trapNpc = { 283092, 283094 };
        int trap = trapNpc[System.Random.Shared.Next(trapNpc.Length)];
        if (GetNpc(trap) is null)
        {
            Spawn(trap, 1031.1f, 466.38f, 445.45f);
            ScheduleTask(() =>
            {
                Spawn(trap == 283092 ? 283171 : 283172, 1031.1f, 466.38f, 445.45f);
                // note: Java also removed the trap NPC (ring.getController().onDelete()) — no scripted
                // NPC-removal API exists yet, so the original trap ring is left in place.
            }, 5000);
        }
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
        _isFinalBuff = false;
        _isHome = true;
    }
}
