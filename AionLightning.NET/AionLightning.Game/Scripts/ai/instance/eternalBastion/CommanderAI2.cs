using System.Linq;
using System.Collections.Generic;
using System;
// CommanderAI2 — Java ai/instance/eternalBastion/CommanderAI2.java. Eternal Bastion commander:
// shouts at HP-percentage breakpoints and calls for aggro-help after enough attacks.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("commander_bastion")]
public sealed class CommanderAI2 : AggressiveNpcAI2
{
    private int _attackCount;
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
        _attackCount++;
        if (_attackCount == 20)
        {
            // note: Java called AggroEventHandler.onAggro(this, creature) here; aggro tracking is owned
            // by NpcAiService, not this script layer.
        }
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
                    case 5:
                        ShoutAttack();
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void ShoutAttack() // MSG Notice 05
    {
        // note: Java broadcast SM_SYSTEM_MESSAGE(1401827) to every online player via
        // World.doOnAllPlayers; world-wide packet broadcast isn't wired at the script layer yet.
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 98, 80, 70, 60, 50, 40, 30, 20, 5 });
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
