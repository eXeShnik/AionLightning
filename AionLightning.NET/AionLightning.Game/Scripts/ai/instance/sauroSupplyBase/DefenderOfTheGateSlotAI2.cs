// DefenderOfTheGateSlotAI2 — Java ai/instance/sauroSupplyBase/DefenderOfTheGateSlotAI2.java. Sauro
// Supply Base gate-slot NPC: shouts + a start-of-fight buff at hp-percentage thresholds.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("slot")]
public sealed class DefenderOfTheGateSlotAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        AddPercent();
        VritraBless(3000);
        base.OnSpawned();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 98 && _percents.Count < 4)
        {
            AddPercent();
        }

        foreach (int percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 98: ShoutStart(); break;
                    case 50: Shout1(); break;
                    case 30: Shout2(); break;
                    case 5: ShoutDied(); break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void ShoutStart() => SendMsg(1501057);
    private void Shout1() => SendMsg(1501058);
    private void Shout2() => SendMsg(1501059);
    private void ShoutDied() => SendMsg(1501060);

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 98, 50, 30, 5 });
    }

    private void VritraGeneralBless(int skillId) => UseSkill(skillId, 60);

    private void VritraBless(int time) => ScheduleTask(() =>
    {
        if (time == 3000) VritraGeneralBless(21135);
    }, time);

    public override void OnDespawned()
    {
        _percents.Clear();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        _percents.Clear();
        base.OnDied();
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
    }
}
