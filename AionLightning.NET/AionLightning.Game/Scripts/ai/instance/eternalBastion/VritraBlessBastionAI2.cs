using System.Linq;
using System.Collections.Generic;
using System;
// VritraBlessBastionAI2 — Java ai/instance/eternalBastion/VritraBlessBastionAI2.java. Eternal
// Bastion NPC: periodically re-casts a legion bless and shouts at HP-percentage breakpoints.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("vritra_bless_bastion")]
public sealed class VritraBlessBastionAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        AddPercent();
        VritraBless();
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
            AddPercent();

        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 98:
                        ShoutStart();
                        break;
                    case 50:
                        Shout1();
                        break;
                    case 30:
                        Shout2();
                        break;
                    case 5:
                        ShoutDied();
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void ShoutStart()
    {
        // note: Java shouted (1501106) via NpcShoutsService.sendMsg; NPC shouts aren't exposed to
        // scripts yet.
    }

    private void Shout1()
    {
        // note: Java shouted (1501107) via NpcShoutsService.sendMsg; NPC shouts aren't exposed to
        // scripts yet.
    }

    private void Shout2()
    {
        // note: Java shouted (1501108) via NpcShoutsService.sendMsg; NPC shouts aren't exposed to
        // scripts yet.
    }

    private void ShoutDied()
    {
        // note: Java shouted (1501105) via NpcShoutsService.sendMsg; NPC shouts aren't exposed to
        // scripts yet.
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 98, 50, 30, 5 });
    }

    private void VritraGeneralBless(int skillId) => UseSkill(skillId);

    private void VritraBless()
    {
        ScheduleTask(() => VritraGeneralBless(20700), 3000, 100000);
    }

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
