// DainatumHealersAI2 — Java ai/instance/illuminaryObelisk/DainatumHealersAI2.java. Support prop
// that casts a repeating heal skill on itself starting 1s after spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dainatum_healers")]
public sealed class DainatumHealersAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(21535, 65), 1000, 10000);
    }

    public override void OnDied()
    {
        // note: Java cancelled its heal-loop task here before calling super; CancelTasks() (called by
        // base.OnDied()) already stops the loop, so no extra action is needed.
        base.OnDied();
    }
}
