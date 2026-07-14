// DorakikiTheBoldAI2 — Java ai/instance/beshmundirTemple/DorakikiTheBoldAI2.java. Shouts on engage
// and clears its own weaken-mark effect on attack-complete/back-home.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("dorakiki_the_bold")]
public sealed class DorakikiTheBoldAI2 : AggressiveNpcAI2
{
    private const int WeakenMarkSkillId = 18901;

    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500079);
        }
    }

    public override void OnAttackComplete()
    {
        base.OnAttackComplete();
        Owner.RemoveEffectBySkillId(WeakenMarkSkillId);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        Owner.RemoveEffectBySkillId(WeakenMarkSkillId);
        _isHome = true;
    }
}
