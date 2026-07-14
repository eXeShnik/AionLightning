// TheOfficerInspectorSayahumAI2 — Java ai/instance/sauroSupplyBase/TheOfficerInspectorSayahumAI2.java.
// Sauro Supply Base boss: shouts + skills at hp-percentage thresholds.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("sayahum")]
public sealed class TheOfficerInspectorSayahumAI2 : AggressiveNpcAI2
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
        if (hpPercentage > 98 && _percents.Count < 11)
        {
            AddPercent();
        }

        foreach (int percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 98: ShoutStart(); Skill2(); break;
                    case 95: Skill3(); break;
                    case 80: Skill2(); break;
                    case 70: Skill3(); Shout1(); break;
                    case 60: Skill2(); break;
                    case 50: Shout1(); Skill3(); break;
                    case 40: Skill2(); break;
                    case 30: Skill3(); Shout2(); break;
                    case 20: Skill2(); break;
                    case 10: Skill3(); break;
                    case 5: Skill2(); ShoutDied(); break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void Skill2() { if (Owner.Target is Player) UseSkill(17228, 65); }
    private void Skill3() { if (Owner.Target is Player) UseSkill(18334, 65); }

    private void ShoutStart() => SendMsg(1501068);
    private void Shout1() => SendMsg(1501069);
    private void Shout2() => SendMsg(1501070);
    private void ShoutDied() => SendMsg(1501071);

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 98, 95, 80, 70, 60, 50, 40, 30, 20, 10, 5 });
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
