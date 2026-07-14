// DainatumBombAI2 — Java ai/instance/illuminaryObelisk/DainatumBombAI2.java. Timed mine prop:
// casts a no-animation skill 6s after spawn, then deletes itself 10s after spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dainatum_mine")]
public sealed class DainatumBombAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java guarded each tick with isAlreadyDead()/cancelTask(); OnDespawned below already
        // cancels both scheduled tasks, so the guard is redundant here.
        ScheduleTask(() => UseSkill(21275, 65), 6000);
        ScheduleTask(() =>
        {
            // note: Java deleted the owner via NpcActions.delete; no scripted delete/despawn API exists yet.
        }, 10000);
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }
}
