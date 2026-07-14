using System.Linq;
using System.Collections.Generic;
using System;
// HyperionAI2 — Java ai/instance/infinityShard/HyperionAI2.java. Infinity Shard boss: three
// independent periodic skill loops once engaged, plus HP-percentage breakpoints that cast skills and
// spawn escalating add waves.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("hyperion")]
public sealed class HyperionAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private readonly List<int> _percents = new();

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
            StartBlasterTask();
            StartEnergyTask();
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 75:
                    case 60:
                    case 55:
                        SpawnHyperionNormal1();
                        break;
                    case 80:
                    case 47:
                        UseSkill(21245);
                        SpawnHyperionEasy();
                        break;
                    case 52:
                    case 35:
                    case 20:
                        UseSkill(21253);
                        SpawnHyperionNormal();
                        break;
                    case 50:
                    case 25:
                        UseSkill(21244);
                        SpawnHyperionHard();
                        break;
                    case 40:
                        UseSkill(21244);
                        break;
                    case 30:
                        CancelEnergyTask();
                        UseSkill(21248);
                        SpawnHyperionHard();
                        break;
                    case 10:
                        UseSkill(21246);
                        SpawnHyperionNormal();
                        break;
                    case 5:
                    case 2:
                        UseSkill(21249);
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void SpawnHyperionEasy()
    {
        Spawn(231096, 148.12894f, 148.34091f, 124.03375f, 105);
        Spawn(233292, 108.5921f, 145.41702f, 114.03043f, 20);
        Spawn(231103, 132.1073f, 127.7515f, 112.1236f, 35);
        Spawn(231103, 129.2615f, 137.8656f, 110.5048f, 82);
        Spawn(231103, 125.4013f, 129.6880f, 112.1227f, 22);
        Spawn(233289, 110.090965f, 128.28905f, 124.15179f, 43);
    }

    private void SpawnHyperionNormal()
    {
        Spawn(233288, 148.12894f, 148.34091f, 124.03375f, 105);
        Spawn(233294, 108.5921f, 145.41702f, 114.03043f, 20);
        Spawn(231103, 150.05635f, 128.56758f, 114.49583f, 16);
        Spawn(231103, 136.9867f, 133.6464f, 112.1236f, 52);
        Spawn(231103, 124.3499f, 145.8533f, 112.1236f, 101);
        Spawn(233296, 110.090965f, 128.28905f, 124.15179f, 43);
    }

    private void SpawnHyperionNormal1()
    {
        Spawn(233292, 148.12894f, 148.34091f, 124.03375f, 105);
        Spawn(233294, 108.5921f, 145.41702f, 114.03043f, 20);
        Spawn(233295, 150.05635f, 128.56758f, 114.49583f, 16);
        Spawn(231103, 140.8518f, 139.3699f, 112.1228f, 63);
        Spawn(231103, 119.3952f, 140.4778f, 112.1228f, 114);
        Spawn(233295, 110.090965f, 128.28905f, 124.15179f, 43);
    }

    private void SpawnHyperionHard()
    {
        Spawn(233288, 148.12894f, 148.34091f, 124.03375f, 105);
        Spawn(233299, 148.12894f, 148.34091f, 124.03375f, 105);
        Spawn(233294, 108.5921f, 145.41702f, 114.03043f, 20);
        Spawn(233298, 150.05635f, 128.56758f, 114.49583f, 16);
        Spawn(231103, 134.50612f, 141.6616f, 112.1228f, 72);
        Spawn(231103, 121.2149f, 133.1198f, 112.1220f, 10);
        Spawn(231103, 125.4013f, 129.68802f, 112.1227f, 22);
        Spawn(233298, 110.090965f, 128.28905f, 124.15179f, 43);
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 80, 75, 60, 55, 52, 50, 40, 35, 30, 25, 20, 10, 5, 2 });
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            // note: Java guarded each tick with isAlreadyDead()/cancelskillTask(); CancelTasks() already
            // stops every loop from the base OnDied() hook, so the guard is redundant here.
            Throw();
        }, 30000, 120000);
    }

    private void StartBlasterTask()
    {
        ScheduleTask(Blaster, 2000, 90000);
    }

    private void StartEnergyTask()
    {
        ScheduleTask(Energy, 10000, 160000);
    }

    // note: Java tracked skillTask/BlasterTask/EnergyTask as separate cancellable Futures so the 30%-HP
    // breakpoint could stop only the energy loop while skill/blaster kept running. ScheduleTask exposes
    // no per-task handle — only CancelTasks() (cancel-everything) — so selective cancellation isn't
    // possible yet; these stay as documented no-ops.
    private void CancelSkillTask() { }
    private void CancelBlasterTask() { }
    private void CancelEnergyTask() { }

    private void Throw()
    {
        UseSkill(21250);
        ScheduleTask(() => UseSkill(21251), 5000);
    }

    private void Blaster()
    {
        UseSkill(21241); // note: Java cast this on getOwner().getTarget(); UseSkill has no target slot.
        GetRandomTarget(); // note: Java fetched a random known-list target here but never used it (dead code in the original).
    }

    private void Energy()
    {
        UseSkill(21247); // note: Java cast this on getOwner().getTarget(); UseSkill has no target slot.
        GetRandomTarget(); // note: Java fetched a random known-list target here but never used it (dead code in the original).
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    private void DespawnAdds()
    {
        // note: Java deleted every live add from a fixed list of npc ids (231096, 233292, 231103,
        // 233289, 233288, 233294, 233296, 233295, 233299, 233298, 231104) via WorldMapInstance.getNpcs +
        // getController().onDelete(); bulk npc-id lookup and scripted despawn aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        AddPercent();
        CancelSkillTask();
        CancelBlasterTask();
        CancelEnergyTask();
        _isHome = true;
        DespawnAdds();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        _percents.Clear();
        DespawnAdds();
        CancelSkillTask();
        CancelBlasterTask();
        CancelEnergyTask();
    }

    public override void OnDied()
    {
        base.OnDied();
        _percents.Clear();
        CancelSkillTask();
        CancelBlasterTask();
        CancelEnergyTask();
        DespawnAdds();
    }
}
