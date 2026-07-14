// OphidanBridgeAI2 — Java ai/instance/ophidanBridge/OphidanBridgeAI2.java. Ophidan Bridge lever:
// opens instance door #47, shouts, and deletes itself once used.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ophidan_bridge")]
public sealed class OphidanBridgeAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java opened instance door #47, shouted message 1401879, and deleted itself
        // (AI2Actions.deleteOwner); instance doors, NPC shouts, and delete-owner aren't wired at the
        // script layer yet.
    }
}
