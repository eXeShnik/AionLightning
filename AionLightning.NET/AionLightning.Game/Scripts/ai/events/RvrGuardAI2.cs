// RvrGuardAI2 — Java ai/events/RvrGuardAI2.java. Silentera Canyon RvR guard: registers attacking
// players for a bonus reward while both faction bosses are alive, within a nightly time window.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rvr_guard")]
public sealed class RvrGuardAI2 : AggressiveNpcAI2
{
    private const int SilenteraCanyonWorldId = 600010000;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (creature is Player && Owner.Position.WorldId == SilenteraCanyonWorldId)
        {
            int hour = DateTime.Now.Hour;
            if (hour is >= 19 and <= 23)
            {
                var bossAsmo = GetNpc(220948);
                var bossElyos = GetNpc(220949);
                if (bossAsmo is not null && bossElyos is not null)
                {
                    // note: Java registered the attacker for a bonus reward via
                    // SiegeService.getInstance().checkRvrPlayerOnEvent — SiegeService isn't reachable
                    // from the script layer.
                }
            }
        }
    }
}
