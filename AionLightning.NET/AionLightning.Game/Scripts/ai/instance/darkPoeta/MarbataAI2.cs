// MarbataAI2 — Java ai/instance/darkPoeta/MarbataAI2.java. Boss that self-buffs once when it first
// gains aggro, and clears the buffs on returning home.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("marbata")]
public sealed class MarbataAI2 : AggressiveNpcAI2
{
    private bool _isStart;

    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        if (!_isStart)
        {
            _isStart = true;
            Buff();
        }
    }

    private void Buff()
    {
        UseSkill(18556);
        UseSkill(18110);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isStart = false;
        Owner.RemoveEffectBySkillId(18556);
        Owner.RemoveEffectBySkillId(18110);
    }
}
