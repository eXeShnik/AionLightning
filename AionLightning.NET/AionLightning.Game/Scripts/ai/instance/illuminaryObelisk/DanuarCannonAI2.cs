// DanuarCannonAI2 — Java ai/instance/illuminaryObelisk/DanuarCannonAI2.java. Illuminary Obelisk
// item-use prop: applies a one-shot buff effect to the player and deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("danuar_cannon")]
public sealed class DanuarCannonAI2 : ActionItemNpcAI2
{
    private bool _canUse = true;

    protected override void HandleUseItemFinish(Player player)
    {
        if (_canUse)
        {
            _canUse = false;
            // note: Java applied a 1h buff effect (21511) directly via SkillEngine.applyEffectDirectly,
            // stopped the player's movement, and deleted itself via AI2Actions.deleteOwner. SkillEngine
            // effect application, movement control and AI2Actions aren't exposed to scripts yet (also
            // overrode pollInstance(SHOULD_REWARD) to refuse the reward grant — no C# equivalent poll
            // exists).
        }
    }
}
