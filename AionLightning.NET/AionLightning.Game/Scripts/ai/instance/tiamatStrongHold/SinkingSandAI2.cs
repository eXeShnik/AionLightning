// SinkingSandAI2 — Java ai/instance/tiamatStrongHold/SinkingSandAI2.java. Zone hazard: casts a
// skill 3s after spawn, then self-kills.
using AionLightning.Game.Ai;

namespace Ai.TiamatStrongHold;

[AiName("sinkingsand")]
public sealed class SinkingSandAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkillThenDie();
    }

    private void UseSkillThenDie()
    {
        ScheduleTask(() =>
        {
            UseSkill(20721);
            // note: Java killed the owner (getController().die()) here — no scripted self-kill API
            // exists yet.
        }, 3000);
    }
}
