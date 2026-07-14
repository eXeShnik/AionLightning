// SinkingSandAI2 — Java ai/instance/dragonLordsRefuge/SinkingSandAI2.java. Ground hazard: one variant
// casts a skill, then both variants force-kill themselves 10s after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("sinkingsandtiamat")]
// 283083, 283084
public sealed class SinkingSandAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (getOwner().Template.NpcId == 283084)
            {
                UseSkill(20965);
            }
            // note: Java force-killed itself via getController().die(); no scripted self-kill API exists
            // yet.
        }, 10000);
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java also called AI2Actions.deleteOwner(this) here; no scripted despawn API exists yet.
    }
}
