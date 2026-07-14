using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Services;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Java services.base.BossDeathListener — Java registered this as an ai2 <c>OnDieEventCallback</c>
/// directly on the boss NPC's AI (<c>Base.addBossListeners</c>). This port has no ai2 callback framework
/// (see CLAUDE.md "Callbacks/AOP" — dropped), so instead this subscribes to the shared DeathEvent bus and
/// looks up which base (if any) owns the dying NPC via <see cref="BaseService.GetBaseNpc"/>, mirroring
/// <see cref="SiegeBossDeathHandler"/>'s exact same shape.
/// </summary>
public sealed class BaseBossDeathHandler(BaseService baseService) : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Victim is not Npc npc) return;
        if (baseService.GetBaseNpc(npc) is not { IsBoss: true } baseNpc) return;

        // Java BossDeathListener.onBeforeDie: race = killer's race, resolved from whichever of
        // Player/PlayerGroup/PlayerAlliance/League/Creature the killer object happened to be. This port's
        // DeathEvent.Killer is already resolved to the single actor that landed the final blow, and only
        // Player carries a Race in this port's Creature hierarchy (see Model.Player's own Race property)
        // — a boss killed by a non-player Creature (e.g. friendly fire from another NPC) has no
        // meaningful capturing race and is left un-captured, matching Java's own null-race no-op path.
        if (e.Killer is not Player killer) return;

        var capturingRace = SiegeRaceExtensions.FromPlayerRace(killer.Race);
        await baseService.CaptureAsync(baseNpc.BaseId, capturingRace, ct);
    }
}
