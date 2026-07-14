// InfinitePainAI2 — Java ai/instance/dragonLordsRefuge/InfinitePainAI2.java. Tiamat add: casts a
// gravitational-disturbance skill 2s after spawning, then self-deletes 5s later.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("infinitepain")]
// 283143, 283144
public sealed class InfinitePainAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            UseSkill(20969);
            // note: Java self-deleted via getController().onDelete() 5s after casting; no scripted despawn
            // API exists yet.
            ScheduleTask(() => { }, 5000);
        }, 2000);
    }
}
