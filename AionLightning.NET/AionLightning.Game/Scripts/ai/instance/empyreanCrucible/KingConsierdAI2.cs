// KingConsierdAI2 — Java ai/instance/empyreanCrucible/KingConsierdAI2.java. Empyrean Crucible boss:
// HP-threshold skill escalation (75%/25%) plus a repeating skill-and-add-spawn cycle once below 75%.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("king_consierd")]
public sealed class KingConsierdAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private bool _isHome = true;

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercents();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        _percents.Clear();
        // note: Java also deleted every live 282378 add via WorldMapInstance.getNpcs(id) +
        // getController().onDelete(); bulk npc-id lookup and scripted despawn aren't exposed to scripts
        // yet.
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        // note: same 282378 add cleanup as OnDespawned.
        base.OnDied();
    }

    public override void OnBackHome()
    {
        CancelTasks();
        AddPercents();
        // note: same 282378 add cleanup as OnDespawned.
        base.OnBackHome();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
        if (_isHome)
        {
            _isHome = false;
            ScheduleTask(() =>
            {
                UseSkill(19691);
                ScheduleTask(() => UseSkill(17954, 29), 4000);
            }, 2000);
            StartBloodThirstTask();
        }
    }

    private void StartBloodThirstTask()
    {
        ScheduleTask(() => UseSkill(19624, 10), 180000); // 3min, need confirm
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            UseSkill(17951, 29);
            ScheduleTask(() =>
            {
                DropAggro();
                if (getOwner().HpPercentage <= 50)
                {
                    var p = getOwner().Position;
                    Spawn(282378, p.X, p.Y, p.Z, (byte)p.Heading);
                    Spawn(282378, p.X, p.Y, p.Z, (byte)p.Heading);
                }
                ScheduleTask(() => UseSkill(17952, 29), 2000);
            }, 3500);
        }, 0, 25000);
    }

    private void DropAggro()
    {
        // note: Java halved the current target's hate entry via AggroList.getAggroInfo(...).setHate(hate
        // / 2) then re-ran think(); aggro-list manipulation is owned by NpcAiService, not this script
        // layer.
    }

    private void CheckPercentage(int percentage)
    {
        foreach (var percent in _percents)
        {
            if (percentage > percent) continue;
            if (percent == 75) StartSkillTask();
            else if (percent == 25) UseSkill(19690);
            _percents.Remove(percent);
            break;
        }
    }

    private void AddPercents()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 25 });
    }
}
