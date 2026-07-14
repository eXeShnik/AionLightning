// ElectrocuteAI2 — Java ai/instance/tiamatStrongHold/ElectrocuteAI2.java. Zone hazard: spams a
// skill every 2s, then self-removes after 10s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("electrocute")]
public sealed class ElectrocuteAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(20757), 0, 2000);
        // note: Java self-removed via getController().onDelete() 10s after spawn — no scripted
        // NPC-removal API exists yet.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
