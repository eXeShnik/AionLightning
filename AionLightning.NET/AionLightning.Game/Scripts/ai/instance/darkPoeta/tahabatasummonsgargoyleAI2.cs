// tahabatasummonsgargoyleAI2 — Java ai/instance/darkPoeta/tahabatasummonsgargoyleAI2.java. Summoned
// gargoyle helper that casts a skill 10s after spawning, then self-deletes 5s later.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("summonsgargoyle")]
public sealed class tahabatasummonsgargoyleAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        StartTimer();
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the scheduled event task
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    private void StartTimer()
    {
        ScheduleTask(() =>
        {
            UseSkill(18219, 50);
            ScheduleTask(() =>
            {
                // note: Java self-deleted via getController().onDelete(); self-delete isn't wired at
                // the script layer yet.
            }, 5000);
        }, 10000);
    }
}
