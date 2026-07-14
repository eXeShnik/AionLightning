// TarosLifebaneAI2 — Java ai/instance/beshmundirTemple/TarosLifebaneAI2.java. Shouts on engage, death,
// and at each of three HP breakpoints.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("taroslifebane")]
public sealed class TarosLifebaneAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500073);
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied();
        _percents.Clear();
        SendMsg(1500075);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isHome = true;
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            SendMsg(1500074);
            _percents.Remove(percent);
            break;
        }
    }
}
