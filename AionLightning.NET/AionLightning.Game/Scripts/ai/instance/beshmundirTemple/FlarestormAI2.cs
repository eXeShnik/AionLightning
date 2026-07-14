// FlarestormAI2 — Java ai/instance/beshmundirTemple/FlarestormAI2.java. Beshmundir Temple boss:
// HP-breakpoint pressure-wave/ash-mantle/orb-of-annihilation phases.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("flarestorm")]
public sealed class FlarestormAI2 : AggressiveNpcAI2
{
    private const int PressureWaveSkillId = 18909;
    private const int AshMantleSkillId = 18997;
    private const int OrbOfAnnihilationSkillId = 18911;

    private bool _isHome = true;
    private bool _isUsingOtherSkill;
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
            SendMsg(1500076);
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the pressure-wave/skill-window/orb tasks
        _percents.Clear();
        SendMsg(1500078);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isHome = true;
        _isUsingOtherSkill = false;
        CancelTasks();
        AddPercent();
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 90, 75, 50, 25 });
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 90:
                    StartPressureWave();
                    break;
                case 75:
                case 50:
                    StartSkillWindow();
                    UseSkill(AshMantleSkillId);
                    break;
                case 25:
                    StartSkillWindow();
                    UseSkill(AshMantleSkillId);
                    StartOrbOfAnnihilation();
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void StartOrbOfAnnihilation()
    {
        ScheduleTask(() =>
        {
            if (!_isHome && !Owner.IsAlreadyDead) UseSkill(OrbOfAnnihilationSkillId);
        }, 20000);
    }

    private void StartPressureWave()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            if (!_isUsingOtherSkill)
            {
                // note: Java never reset this flag on its own — only the skill-window task (below) clears
                // it, so once set here it stays true until a 75/50/25 threshold also fires. Preserved as-is.
                _isUsingOtherSkill = true;
                UseSkill(PressureWaveSkillId);
            }
        }, 45000, 45000);
    }

    private void StartSkillWindow()
    {
        _isUsingOtherSkill = true;
        ScheduleTask(() =>
        {
            if (!_isHome && !Owner.IsAlreadyDead) _isUsingOtherSkill = false;
        }, 12000);
    }
}
