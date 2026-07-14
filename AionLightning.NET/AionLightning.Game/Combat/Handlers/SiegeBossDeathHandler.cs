using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Java services.siegeservice.SiegeBossDeathListener — Java registered this as an ai2
/// <c>OnDieEventCallback</c> directly on the boss NPC's AI (<c>Siege.registerSiegeBossListeners</c>).
/// This port has no ai2 callback framework (see CLAUDE.md "Callbacks/AOP" — dropped), so instead this
/// subscribes to the shared DeathEvent bus and looks up which active siege (if any) owns the dying NPC
/// via <see cref="SiegeService.GetSiegeNpc"/>.
/// </summary>
public sealed class SiegeBossDeathHandler(SiegeService siegeService) : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Victim is not Npc npc) return;
        if (siegeService.GetSiegeNpc(npc) is not { IsBoss: true } siegeNpc) return;
        if (siegeService.GetSiege(siegeNpc.SiegeId) is not { } siege) return;
        if (siege.Boss?.Npc.ObjectId != npc.ObjectId) return; // not the boss currently tracked by this siege

        siege.BossKilled = true;
        await siegeService.StopSiegeAsync(siege.SiegeLocationId, ct);
    }
}
