// IdgelTurreteAI2 — Java ai/instance/idgelResearchCenter/IdgelTurreteAI2.java. Idgel Research
// Center item-use prop: applies a one-shot buff effect to the player and deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("turret_idgellab")]
public sealed class IdgelTurreteAI2 : ActionItemNpcAI2
{
    private bool _canUse = true;

    protected override void HandleUseItemFinish(Player player)
    {
        if (_canUse)
        {
            _canUse = false;
            // note: Java applied a 3-minute buff effect (21123) directly via SkillEngine.applyEffectDirectly,
            // sent a bright-yellow center message, stopped the player's movement, and scheduled a respawn +
            // deleted itself via AI2Actions. SkillEngine effect application, PacketSendUtility, movement
            // control and AI2Actions aren't exposed to scripts yet (also overrode pollInstance(SHOULD_REWARD)
            // to refuse the reward grant — no C# equivalent poll exists).
        }
    }
}
