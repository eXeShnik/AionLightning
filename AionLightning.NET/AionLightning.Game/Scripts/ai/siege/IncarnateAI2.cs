// IncarnateAI2 — Java ai/siege/IncarnateAI2.java. Siege incarnate boss: every 10s strips the
// "deity avatar" morph from nearby high-rank enemies while the instance-death-avoidance config is
// enabled.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("incarnate")]
public sealed class IncarnateAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(ScanForDeityAvatars, 10000, 10000);
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    private void ScanForDeityAvatars()
    {
        // note: Java (gated by SiegeConfig.SIEGE_IDA_ENABLED) scanned every player in its known-list above
        // STAR4_OFFICER abyss rank, ended any active "deity avatar" morph effect on them, and broadcast an
        // SM_MESSAGE notice. SiegeConfig, the deity-avatar effect flag, and known-list player broadcast
        // aren't exposed to the script layer yet.
    }
}
