// RetributiveEffigyAI2 — Java ai/instance/idgelResearchCenter/RetributiveEffigyAI2.java. Casts a
// one-shot retaliation skill the first time it's attacked.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("retributiveeffigy")]
public sealed class RetributiveEffigyAI2 : AggressiveNpcAI2
{
    private bool _isAggred;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isAggred)
        {
            _isAggred = true;
            UseSkill(19406, 60);
        }
    }
}
