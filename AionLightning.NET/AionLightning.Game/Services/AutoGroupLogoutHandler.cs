using AionLightning.Commons.Events;
using AionLightning.Game.Events;

namespace AionLightning.Game.Services;

/// <summary>
/// Feeds <see cref="AutoGroupService.OnPlayerLogout"/> from the shared event bus (Java
/// <c>AutoGroupService.onPlayerLogOut</c>, called directly from Java's player-logout path). This port
/// has no player-logout hook wired up yet anywhere (see <see cref="KiskService"/>'s doc) —
/// <see cref="PlayerLeftWorldEvent"/> exists but nothing currently publishes it. This handler is
/// forward-compatible dead code until a logout path publishes that event; it does not change behavior
/// today.
/// </summary>
public sealed class AutoGroupLogoutHandler(AutoGroupService autoGroupService) : IEventHandler<PlayerLeftWorldEvent>
{
    public ValueTask HandleAsync(PlayerLeftWorldEvent e, CancellationToken ct)
    {
        autoGroupService.OnPlayerLogout(e.Player);
        return ValueTask.CompletedTask;
    }
}
