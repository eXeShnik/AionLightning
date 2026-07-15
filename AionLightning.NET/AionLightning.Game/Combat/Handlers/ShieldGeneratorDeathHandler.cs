using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Java ai.siege.ShieldNpcAI2.handleDespawned's fortress-shield-drop effect. Java wired this directly
/// off the AI script's despawn hook; this port has no ai2 callback framework (see CLAUDE.md "Callbacks/
/// AOP" — dropped), so instead this subscribes to the shared DeathEvent bus and asks
/// <see cref="ShieldService"/> whether the dying NPC was a tracked shield generator, mirroring
/// <see cref="SiegeBossDeathHandler"/>/<see cref="VortexGeneratorDeathHandler"/>'s exact same shape.
/// </summary>
public sealed class ShieldGeneratorDeathHandler(ShieldService shieldService) : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Victim is not Npc npc) return;
        await shieldService.OnGeneratorDeathAsync(npc, ct);
    }
}
