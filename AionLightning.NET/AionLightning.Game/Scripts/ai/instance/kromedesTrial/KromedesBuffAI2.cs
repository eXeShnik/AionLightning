// KromedesBuffAI2 — Java ai/instance/kromedesTrial/KromedesBuffAI2.java. Use-item buff totems:
// apply a per-npcId buff effect to the using player.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("krbuff")]
public sealed class KromedesBuffAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java applied a per-npcId buff effect directly (730336->19216, 730337->19217,
        // 730338->19218, 730339->19219 via SkillEngine.applyEffectDirectly), sent a matching
        // SM_SYSTEM_MESSAGE, and for two of the four ids also deleted the owner (AI2Actions
        // .deleteOwner). SkillEngine.applyEffectDirectly, PacketSendUtility, and scripted despawn
        // aren't exposed to the script layer yet.
    }
}
