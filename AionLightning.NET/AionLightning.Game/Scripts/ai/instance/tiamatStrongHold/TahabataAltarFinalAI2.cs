// TahabataAltarFinalAI2 — Java ai/instance/tiamatStrongHold/TahabataAltarFinalAI2.java. Final
// lava-floor hazard: spams a skill every 2s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("tahabataaltar2")]
public sealed class TahabataAltarFinalAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(20972), 0, 2000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
