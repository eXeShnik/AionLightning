// EmpyreanAdministratorArminosAI2 — Java ai/instance/empyreanCrucible/EmpyreanAdministratorArminosAI2.java.
// Narrates a crucible stage-start sequence via timed NPC shouts keyed by npc id.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("empadministratorarminos")]
public sealed class EmpyreanAdministratorArminosAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java fired a scripted sequence of NpcShoutsService shouts (timed 8s-118s apart) keyed by
        // npc id (217744 or 217749) to narrate the stage-start event; NPC shouts aren't exposed to
        // scripts yet.
    }
}
