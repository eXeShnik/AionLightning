// SpringAI2 — Java ai/siege/SpringAI2.java. Race-locked healing spring: every 5s heals the nearest
// same-race, non-full-HP creature within 10m that lacks the spring's ward-off buff.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("spring")]
public sealed class SpringAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        ScheduleTask(CheckForHeal, 5000, 5000);
    }

    private void CheckForHeal()
    {
        // note: Java scanned its known-list every 5s for the first non-dead, not-fully-healed, same-race
        // creature within 10m lacking abnormal effect 19116 (a SiegeNpc or an online Player), then
        // self-targeted and cast heal skill 19116 on it. Known-list enumeration and NpcObjectTemplate race
        // comparison aren't exposed to the script layer yet.
    }
}
