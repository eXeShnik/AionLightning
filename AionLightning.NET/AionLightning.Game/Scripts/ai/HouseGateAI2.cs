// HouseGateAI2 — Java ai/HouseGateAI2.java. Teleports a house owner/group member in or out of a
// house/studio instance after an accept/deny confirmation dialog.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("housegate")]
public sealed class HouseGateAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java resolved the creator's house/studio via HousingService, confirmed via an AI2Request
        // (SM_QUESTION_WINDOW), then teleported the responder in/out through TeleportService2 — handling
        // zone-recall restrictions and personal-instance registration along the way. HousingService and
        // AI2Request polling aren't wired at the script layer yet.
    }
}
