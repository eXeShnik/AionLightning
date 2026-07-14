// SiegeFortressGateAI2 — Java ai/siege/SiegeFortressGateAI2.java. Fortress gate NPC: on use,
// teleports the requester to stand near the responder if within range.
// note: Java also overrode pollInstance to refuse decay/respawn — no C# equivalent exists.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("fortressgate")]
public sealed class SiegeFortressGateAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java staged an AI2Request confirmation that teleported the requester to stand behind/in
        // front of the responder when within 10m (TeleportService2.moveToTargetWithDistance), else sent a
        // "you too far away" message. AI2Request/TeleportService2/MathUtil/PositionUtil aren't ported to
        // the script layer yet.
    }

    public override void OnDialogFinish(Player player)
    {
    }
}
