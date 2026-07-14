// StrangeCreatureAI2 — Java ai/instance/empyreanCrucible/StrangeCreatureAI2.java. Short-lived hazard
// add: shouts and casts once shortly after spawn, then self-removes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("strange_creature")]
public sealed class StrangeCreatureAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead) return;
            // note: Java shouted 341444 via NpcShoutsService here; NPC shouts aren't exposed to scripts
            // yet.
            UseSkill(17914, 34);
        }, 500);
        ScheduleTask(() =>
        {
            // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, 6500);
    }
}
