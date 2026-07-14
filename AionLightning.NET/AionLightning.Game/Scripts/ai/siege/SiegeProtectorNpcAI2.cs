// SiegeProtectorNpcAI2 — Java ai/siege/SiegeProtectorNpcAI2.java. Siege protector: on death, one
// specific npc id spawns a follow-up "claw" NPC that despawns itself 5 minutes later.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("siege_protector")]
public sealed class SiegeProtectorNpcAI2 : SiegeNpcAI2
{
    private const int ClawNpcId = 701237;

    public override void OnDied()
    {
        base.OnDied();
        if (Owner.Template.NpcId == 259614)
        {
            Spawn(ClawNpcId, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
            ScheduleTask(DespawnClaw, 60000 * 5);
        }
    }

    private void DespawnClaw()
    {
        // note: Java deleted the spawned claw NPC (found via world-instance lookup by npc id) 5 minutes
        // after spawning it; NPC self-removal isn't exposed to the script layer yet.
    }
}
