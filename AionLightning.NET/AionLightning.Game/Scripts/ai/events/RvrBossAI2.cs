// RvrBossAI2 — Java ai/events/RvrBossAI2.java. Silentera Canyon RvR boss: registers attackers for a
// bonus reward, and on death despawns its paired enemy-faction boss and schedules a 12h respawn.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rvr_boss")]
public sealed class RvrBossAI2 : AggressiveNpcAI2
{
    private const int SilenteraCanyonWorldId = 600010000;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java registered the attacking player for a bonus reward via
        // SiegeService.getInstance().checkRvrPlayerOnEvent when in Silentera Canyon, and left a TODO to
        // spawn defensive guards for canyon bosses. SiegeService isn't reachable from the script layer.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted the paired enemy-faction boss NPC via getController().onDelete() when in
        // Silentera Canyon — NPC self-despawn isn't exposed to the script layer.
        if (Owner.Position.WorldId == SilenteraCanyonWorldId)
        {
            ScheduleTask(() =>
            {
                Spawn(219641, 658.7087f, 795.21857f, 293.14087f, 7);
                Spawn(219642, 657.95105f, 737.5624f, 293.19818f, 0);
            }, 43200000);
        }
        // note: Java also mailed every participating player a reward via SystemMailService and cleared
        // SiegeService's RvR event-player list within a 19:00-23:00 window — neither SystemMailService
        // nor SiegeService is reachable from the script layer.
    }
}
