// DebarimTheOmnipotentAI2 — Java ai/worlds/sarpan/DebarimTheOmnipotentAI2.java. Toggles a (bugged,
// inverted-display) door on first aggro and reopens it on death/back-home.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

// note: Java toggled door 480 (a "bugged door" whose displayed open-state is the opposite of the
// actual one) on first player aggro and reopened it on death/back-home; doors aren't exposed to
// scripts yet (see World/Geo door stub).
[AiName("debarim")]
public sealed class DebarimTheOmnipotentAI2 : AggressiveNpcAI2
{
    private bool _isStart;

    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        if (creature is Player && !_isStart)
        {
            _isStart = true;
        }
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isStart = false;
    }
}
