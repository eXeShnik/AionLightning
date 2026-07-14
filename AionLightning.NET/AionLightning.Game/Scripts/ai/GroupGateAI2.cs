// GroupGateAI2 — Java ai/GroupGateAI2.java. Teleports a group's members (or an invasion-rift
// group) to a hardcoded destination after an accept/deny confirmation dialog.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("groupgate")]
public sealed class GroupGateAI2 : NpcAi2
{
    private const int CancelDialogMeters = 9;

    public override void OnDialogStart(Player player)
    {
        // note: Java ran an AI2Request confirmation (SM_QUESTION_WINDOW, cancel range CancelDialogMeters)
        // then teleported the responder via TeleportService2 to hardcoded npcId-keyed destinations
        // (group gates, binding group gates, invasion-rift gates). AI2Request polling isn't wired at the
        // script layer yet.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
    }

    public override void OnDialogFinish(Player player)
    {
    }
}
