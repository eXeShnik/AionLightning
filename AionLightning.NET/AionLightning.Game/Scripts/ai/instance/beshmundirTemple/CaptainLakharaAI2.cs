// CaptainLakharaAI2 — Java ai/instance/beshmundirTemple/CaptainLakharaAI2.java. Beshmundir Temple
// boss: HP-breakpoint Divine Grasp pull/spin sequence and a low-HP berserk-chance rage phase.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("captainlakhara")]
public sealed class CaptainLakharaAI2 : AggressiveNpcAI2
{
    private const int ArmsUpPullSkillId = 18890;
    private const int PullPlayerSkillId = 18995;
    private const int SpinSlashSkillId = 19090;
    private const int BerserkSkillId = 18891;
    private const int DivineGraspIntervalMs = 60000;
    private const int RageIntervalMs = 25000;

    private bool _isHome = true;
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500042);
            StartDivineGrasp();
            // note: Java also called callForHelp(36) here; aggro spreading isn't exposed to scripts
            // (see AggressiveNpcAI2.CallForHelp).
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the divine-grasp/rage tasks
        SendMsg(1500044);
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        _isHome = true;
        CancelTasks();
        base.OnBackHome();
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 25, 10 });
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 25:
                    SendMsg(1500043);
                    // note: Java stopped only the divine-grasp task here; NpcAi2 has no per-task cancel,
                    // so it keeps running until the next OnDied/OnDespawned/OnBackHome CancelTasks() call.
                    break;
                case 10:
                    StartRage();
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void StartDivineGrasp()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            UseSkill(ArmsUpPullSkillId); // arms up pull after three seconds

            ScheduleTask(() =>
            {
                if (!_isHome && !Owner.IsAlreadyDead) UseSkill(PullPlayerSkillId); // pull player
            }, 4500);
            ScheduleTask(() =>
            {
                if (!_isHome && !Owner.IsAlreadyDead) UseSkill(SpinSlashSkillId); // Drehschmetterschlag #1
            }, 6500);
            ScheduleTask(() =>
            {
                if (!_isHome && !Owner.IsAlreadyDead) UseSkill(SpinSlashSkillId); // Drehschmetterschlag #2
            }, 8500);
            ScheduleTask(() =>
            {
                // note: Java resumed the fight via AggroList.getMostHated()/getMoveController()/
                // getGameStats() renew-*Time()/handleMoveValidate(); aggro tracking, movement control, and
                // attack-timer internals aren't exposed to scripts — NpcAiService owns resuming combat.
            }, 10500);
        }, DivineGraspIntervalMs, DivineGraspIntervalMs);
    }

    private void StartRage()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead && Random.Shared.Next(1, 3) == 2) UseSkill(BerserkSkillId);
        }, RageIntervalMs, RageIntervalMs);
    }
}
