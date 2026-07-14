// TheOfficerInspectorOvankaAI2 — Java ai/instance/sauroSupplyBase/TheOfficerInspectorOvankaAI2.java.
// Sauro Supply Base boss: shouts + skills at hp-percentage thresholds, spawning support helpers at
// three of them.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ovanka")]
public sealed class TheOfficerInspectorOvankaAI2 : AggressiveNpcAI2
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
                    case 98: ShoutStart(); Skill1(); break;
                    case 95: Skill2(); break;
                    case 80: Shout2(); SpawnSupport(); break;
                    case 70: Skill3(); break;
                    case 60: Skill4(); break;
                    case 50: Shout2(); SpawnSupport(); break;
                    case 40: Skill2(); break;
                    case 30: Shout1(); Skill5(); break;
                    case 20: Skill6(); break;
                    case 10: Shout2(); SpawnSupport(); break;
                    case 5: ShoutDied(); Skill6(); break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void Skill1() { if (Owner.Target is Player) UseSkill(18159, 65); }
    private void Skill2() { if (Owner.Target is Player) UseSkill(17446, 65); }
    private void Skill3() { if (Owner.Target is Player) UseSkill(17332, 65); }
    private void Skill4() { if (Owner.Target is Player) UseSkill(17320, 65); }
    private void Skill5() { if (Owner.Target is Player) UseSkill(18158, 65); }
    private void Skill6() { if (Owner.Target is Player) UseSkill(18160, 65); }

    private void ShoutStart() => SendMsg(1501064);
    private void Shout1() => SendMsg(1501065);
    private void Shout2() => SendMsg(1501066);
    private void ShoutDied() => SendMsg(1501067);

    private void SpawnSupport()
    {
        if (GetNpc(233286) is null)
        {
            Spawn(233286, Owner.Position.X + 1, Owner.Position.Y - 1, Owner.Position.Z);
            Spawn(233286, Owner.Position.X - 1, Owner.Position.Y + 1, Owner.Position.Z);
        }
    }

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
