// GravityTornadoAI2 — Java ai/instance/dragonLordsRefuge/GravityTornadoAI2.java. Gravity-crusher
// remnant: casts its pull skill on a fixed 6s cycle for as long as it's alive.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("tiamat_tornado")]
// 283140
public sealed class GravityTornadoAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(20966), 0, 6000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
