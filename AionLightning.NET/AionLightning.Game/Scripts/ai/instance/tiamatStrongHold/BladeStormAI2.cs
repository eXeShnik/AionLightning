// BladeStormAI2 — Java ai/instance/tiamatStrongHold/BladeStormAI2.java. Spinning-blade hazard NPC:
// spams a skill every second, then self-removes 10s after spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("bladestorm")]
public sealed class BladeStormAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(20748), 0, 1000);
        // note: Java's despawn() deleted the owner via getController().onDelete() after 10s — no
        // scripted NPC-removal API exists yet, so the hazard keeps spinning instead of self-clearing.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
