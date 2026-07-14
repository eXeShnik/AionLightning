// GarnonQ20060AI2 — Java ai/quests/GarnonQ20060AI2.java. Q20060 quest NPC: despawns itself 3 minutes
// after spawning, replacing itself with a follow-up NPC.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("Q20060")]
public sealed class GarnonQ20060AI2 : NpcAi2
{
    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            Spawn(800020, 442.279f, 464.349f, 341.520f, 20);
            // note: Java then deleted itself via getController().onDelete() — NPC self-despawn isn't
            // exposed to the script layer.
        }, 60000 * 3);
    }
}
