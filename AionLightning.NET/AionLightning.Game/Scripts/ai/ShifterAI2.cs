// ShifterAI2 — Java ai/ShifterAI2.java. Use-item NPC that plays an emote after the item-use
// flow finishes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("shifter")]
public class ShifterAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        base.HandleUseItemFinish(player);
        // note: Java broadcast an SM_EMOTION(EMOTE, 144) after finishing the use-item flow;
        // PacketSendUtility isn't wired at the script layer yet.
    }
}
