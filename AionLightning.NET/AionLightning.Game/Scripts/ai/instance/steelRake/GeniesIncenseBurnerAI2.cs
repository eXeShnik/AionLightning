// GeniesIncenseBurnerAI2 — Java ai/instance/steelRake/GeniesIncenseBurnerAI2.java. Use-item incense
// burner that targets itself and casts a skill when used.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("geniesincenseburner")]
public sealed class GeniesIncenseBurnerAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java targeted itself before casting (AI2Actions.targetSelf); self-targeting isn't
        // wired at the script layer yet.
        UseSkill(18465);
    }
}
