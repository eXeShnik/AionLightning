// MagePreceptorAI2 — Java ai/instance/empyreanCrucible/MagePreceptorAI2.java. Empyrean Crucible add:
// HP-threshold skill escalation (75%/50%/25%) that spawns two helper adds at 50%.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("mage_preceptor")]
public sealed class MagePreceptorAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercents();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        DespawnAdds();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        DespawnAdds();
        base.OnDied();
    }

    public override void OnBackHome()
    {
        AddPercents();
        DespawnAdds();
        base.OnBackHome();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    private void StartEvent(int percent)
    {
        if (percent is 50 or 25)
            UseSkill(19606, 10);

        switch (percent)
        {
            case 75:
                // note: Java targeted a random living known player within 37m; known-list scanning isn't
                // exposed to scripts yet.
                UseSkill(19605, 10);
                break;
            case 50:
                ScheduleTask(() =>
                {
                    if (getOwner().IsAlreadyDead) return;
                    UseSkill(19609, 10);
                    ScheduleTask(() =>
                    {
                        var p = getOwner().Position;
                        Spawn(282364, p.X, p.Y, p.Z, (byte)p.Heading);
                        Spawn(282363, p.X, p.Y, p.Z, (byte)p.Heading);
                        ScheduleSkill(2000);
                    }, 4500);
                }, 3000);
                break;
            case 25:
                ScheduleSkill(3000);
                ScheduleSkill(9000);
                ScheduleSkill(15000);
                break;
        }
    }

    private void ScheduleSkill(int delay)
    {
        ScheduleTask(() =>
        {
            if (!getOwner().IsAlreadyDead)
                UseSkill(19605, 10);
        }, delay);
    }

    private void CheckPercentage(int percentage)
    {
        foreach (var percent in _percents)
        {
            if (percentage > percent) continue;
            _percents.Remove(percent);
            StartEvent(percent);
            break;
        }
    }

    private void AddPercents()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }

    private void DespawnAdds()
    {
        // note: Java deleted the live 282364/282363 adds via WorldMapInstance.getNpc(id) +
        // getController().onDelete(); instance npc lookup and scripted despawn aren't exposed to scripts
        // yet.
    }
}
