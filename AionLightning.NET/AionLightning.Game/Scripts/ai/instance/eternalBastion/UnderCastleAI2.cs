using System.Linq;
using System.Collections.Generic;
using System;
// UnderCastleAI2 — Java ai/instance/eternalBastion/UnderCastleAI2.java. Eternal Bastion structure:
// shouts at HP-percentage breakpoints and a final destroy shout at 0%.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("under_castle_bastion")]
public sealed class UnderCastleAI2 : GeneralNpcAI2
{
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        AddPercent();
        base.OnSpawned();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 98 && _percents.Count < 9)
            AddPercent();

        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 98:
                    case 80:
                    case 70:
                    case 60:
                    case 50:
                    case 40:
                    case 30:
                    case 20:
                        ShoutAttack();
                        break;
                    case 0:
                        ShoutDestroy();
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void ShoutAttack() // MSG Notice 01
    {
        // note: Java broadcast SM_SYSTEM_MESSAGE(1401823) to every online player via
        // World.doOnAllPlayers; world-wide packet broadcast isn't wired at the script layer yet.
    }

    private void ShoutDestroy() // MSG Notice 02
    {
        // note: Java broadcast SM_SYSTEM_MESSAGE(1401824) to every online player via
        // World.doOnAllPlayers; world-wide packet broadcast isn't wired at the script layer yet.
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 98, 80, 70, 60, 50, 40, 30, 20, 0 });
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
