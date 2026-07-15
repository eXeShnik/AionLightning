using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Java services.vortexservice.GeneratorDestroyListener — Java registered this as an ai2
/// <c>OnDieEventCallback</c> directly on the generator NPC's AI (DimensionalVortex.registerSiegeBossListeners).
/// This port has no ai2 callback framework (see CLAUDE.md "Callbacks/AOP" — dropped), so instead this
/// subscribes to the shared DeathEvent bus and looks up which vortex location (if any) owns the dying
/// NPC via <see cref="VortexService.GetLocationByGeneratorNpc"/>, mirroring
/// <see cref="BaseBossDeathHandler"/>/<see cref="SiegeBossDeathHandler"/>'s exact same shape.
/// </summary>
public sealed class VortexGeneratorDeathHandler(VortexService vortexService) : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Victim is not Npc npc) return;
        if (vortexService.GetLocationByGeneratorNpc(npc) is not { } location) return;

        location.GeneratorDestroyed = true;
        await vortexService.EndInvasionAsync(location.Id, ct);
    }
}
