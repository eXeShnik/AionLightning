// DavlinsApprenticeAI2 — Java ai/instance/argentManor/DavlinsApprenticeAI2.java. Scripted walker
// NPC that despawns itself once it reaches waypoint 5 of its walk route.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("davlins_apprentice")]
public sealed class DavlinsApprenticeAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java cleared the spawn template's walker id here; canThink() always returned false
        // (no C# equivalent). SpawnTemplate.setWalkerId isn't exposed to scripts.
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java checked MoveController.getCurrentPoint() == 5 and, if so, cleared the walker id,
        // stopped WalkManager routing, and deleted itself via AI2Actions.deleteOwner. Walk-point tracking
        // and WalkManager/AI2Actions aren't exposed to scripts yet.
    }
}
