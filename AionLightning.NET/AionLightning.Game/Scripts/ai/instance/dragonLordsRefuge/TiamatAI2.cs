// TiamatAI2 — Java ai/instance/dragonLordsRefuge/TiamatAI2.java. Dragon Lords' Refuge end boss:
// escalating HP-threshold add waves (Divisive Creation / Gravity Crusher / Infinite Pain) plus a
// periodic atrocity-skill cast.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tiamat")]
// 219361
public sealed class TiamatAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private bool _isHome = true;
    private bool _isSinkingFlag;
    private int _variable = 95;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
        CheckPercentage(getOwner().HpPercentage);
    }

    public override void OnAttackComplete()
    {
        base.OnAttackComplete();
        // note: Java's startSlickTask (the only place that ever set isSinkingFlag true) was already
        // commented out in the source, so this branch is dead code there too — kept for structural parity.
        if (_isSinkingFlag)
        {
            _variable += 5;
            SpawnSinkingSand(_variable);
            if (_variable == 130) _variable = 0;
            if (_variable == 20)
            {
                _isSinkingFlag = false;
                _variable = 95;
            }
        }
    }

    private void StartPainTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
                CancelTasks();
            else
                SpawnInfinitePain();
        }, 25000, 80000);
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
                CancelTasks();
            else
                AtrocityEvent();
        }, 10000, 25000);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 50:
                    CancelTasks();
                    SpawnDivisiveCreation();
                    break;
                case 30:
                    SpawnGravityCrusher();
                    break;
                case 25:
                    StartPainTask();
                    SpawnGravityCrusher();
                    break;
                case 20:
                case 15:
                case 10:
                    SpawnGravityCrusher();
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void AtrocityEvent()
    {
        int var = Random.Shared.Next(3);
        int skill = 20922 + var * 2; // 20922/20924/20926, left,central,right
        SpawnAtrocityNpcs(var);
        UseSkill(skill); // note: Java cast this without animation via SkillEngine directly.
    }

    private void SpawnAtrocityNpcs(int var)
    {
        switch (var)
        {
            case 0:
                Spawn(283237, 445.0000f, 550.7000f, 417.4000f, 0);
                break;
            case 2:
                Spawn(283244, 454.1000f, 474.9000f, 417.4000f, 0);
                break;
            case 1:
                Spawn(283241, 457.8000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 462.3000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 469.7000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 466.6000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 473.6000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 479.3000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 475.8000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 491.2000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 482.7000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 485.2000f, 514.6000f, 417.4000f, 0);
                Spawn(283241, 488.1000f, 514.6000f, 417.4000f, 0);
                break;
        }
    }

    private void SpawnSinkingSand(float heading)
    {
        // note: Java converted `heading` via MathUtil.convertHeadingToDegree into a radian direction and
        // rippled 10 pairs of sinking-sand markers (283329/283330) outward from itself; that heading-to-
        // degree conversion isn't ported to the script layer yet.
    }

    private void SpawnDivisiveCreation()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            DespawnAdds();
            Spawn(283139, 464.24f, 462.26f, 417.4f, 18);
            Spawn(283139, 542.79f, 465.03f, 417.4f, 43);
            Spawn(283139, 541.79f, 563.71f, 417.4f, 74);
            Spawn(283139, 465.79f, 565.43f, 417.4f, 100);
        }, 80000, 45000);
    }

    private void SpawnGravityCrusher()
    {
        DespawnAdds();
        Spawn(283141, 464.24f, 462.26f, 417.4f, 18);
        Spawn(283141, 542.79f, 465.03f, 417.4f, 43);
        Spawn(283141, 541.79f, 563.71f, 417.4f, 74);
        Spawn(283141, 465.79f, 565.43f, 417.4f, 100);
    }

    private void SpawnInfinitePain()
    {
        DespawnAdds();
        Spawn(283143, 508.32f, 515.18f, 417.4f, 0);
        Spawn(283144, 508.32f, 515.18f, 417.4f, 0);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 50, 30, 25, 20, 15, 10 });
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        _percents.Clear();
        DespawnAdds();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
        DespawnAdds();
        CancelTasks();
        _isHome = true;
    }

    private void DespawnAdds()
    {
        // note: Java deleted every live 283141/283139/283140 add via WorldMapInstance.getNpcs(id) +
        // getController().onDelete(); bulk npc-id lookup and scripted despawn aren't exposed to scripts
        // yet.
    }

    public override void OnDied()
    {
        _percents.Clear();
        DespawnAdds();
        base.OnDied();
    }
}
