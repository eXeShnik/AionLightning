using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Routes creature deaths that occur inside an instance channel to that channel's script handler
/// (Java <c>InstanceHandler.onDie(Npc)</c> / <c>onDie(Player, lastAttacker)</c>). No-op for deaths
/// in the open world (InstanceId == 0).
/// </summary>
public sealed class InstanceDeathHandler(InstanceService instanceService) : IEventHandler<DeathEvent>
{
    public ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        switch (e.Victim)
        {
            case Npc npc:
                instanceService.OnNpcDeath(npc);
                break;
            case Player player:
                instanceService.OnPlayerDeath(player, e.Killer);
                break;
        }
        return ValueTask.CompletedTask;
    }
}
