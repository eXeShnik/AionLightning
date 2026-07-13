using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using Microsoft.Extensions.Logging;
using WorldMapInstanceType = AionLightning.Game.World.WorldMapInstance;

namespace AionLightning.Game.Services;

/// <summary>
/// Routes NPC-portal interaction (Java <c>PortalService.port</c>): resolves the portal NPC's destination,
/// allocates or re-enters an instance channel when the destination is instanced, and hands the actual
/// move off to <see cref="TeleportService"/> — the single move choke point.
/// </summary>
public sealed class PortalService
{
    private readonly IDataManager _dataManager;
    private readonly InstanceService _instanceService;
    private readonly TeleportService _teleport;
    private readonly ILogger<PortalService> _log;

    public PortalService(IDataManager dataManager, InstanceService instanceService,
        TeleportService teleport, ILogger<PortalService> log)
    {
        _dataManager     = dataManager;
        _instanceService = instanceService;
        _teleport        = teleport;
        _log             = log;
    }

    /// <summary>
    /// Attempts to use <paramref name="portalNpc"/> as a portal for <paramref name="player"/>.
    /// Returns false if the NPC is not a portal, so the caller can fall back to normal dialog handling.
    /// </summary>
    // note: Java's PortalService.port also gates entry on per-portal-path level/quest/kinah/item
    // requirements, group-size requirements and per-instance re-entry cooldowns (PortalReq /
    // InstanceCooltime). This port's PortalData only carries destination coordinates per race
    // (race gating is already applied by GetPortalLocation), so those additional gates are not
    // modelled here yet — do not invent checks against data that doesn't exist.
    public async ValueTask<bool> UsePortalAsync(Player player, Npc portalNpc, CancellationToken ct)
    {
        var loc = _dataManager.Portals.GetPortalLocation(portalNpc.Template.NpcId, player.Race);
        if (loc is null) return false;

        int worldId = loc.Value.WorldId;

        if (!_dataManager.WorldMaps.IsInstance(worldId))
        {
            await _teleport.TeleportToAsync(player, worldId, 0,
                loc.Value.X, loc.Value.Y, loc.Value.Z, loc.Value.Heading, portAnimation: 0, ct);
            return true;
        }

        var instance = ResolveInstance(player, worldId);
        await _teleport.TeleportToAsync(player, worldId, instance.InstanceId,
            loc.Value.X, loc.Value.Y, loc.Value.Z, loc.Value.Heading, portAnimation: 0, ct);

        var group = player.Group;
        if (group is not null)
        {
            foreach (var member in group.Members)
            {
                if (member.ObjectId == player.ObjectId) continue;
                await _teleport.TeleportToAsync(member, worldId, instance.InstanceId,
                    loc.Value.X, loc.Value.Y, loc.Value.Z, loc.Value.Heading, portAnimation: 0, ct);
            }
        }

        return true;
    }

    /// <summary>
    /// Finds the channel the player (or their group) is already registered to, or allocates a new one
    /// (Java's <c>case 0</c>/<c>case 6</c> re-entry-then-register branches, collapsed to solo/group).
    /// </summary>
    private WorldMapInstanceType ResolveInstance(Player player, int worldId)
    {
        var existing = _instanceService.GetRegisteredInstance(worldId, player.ObjectId);
        if (existing is not null) return existing;

        var group = player.Group;
        if (group is null)
        {
            var solo = _instanceService.GetNextAvailableInstance(worldId);
            _instanceService.RegisterPlayerWithInstance(solo, player);
            return solo;
        }

        foreach (var member in group.Members)
        {
            existing = _instanceService.GetRegisteredInstance(worldId, member.ObjectId);
            if (existing is not null) return existing;
        }

        var created = _instanceService.GetNextAvailableInstance(worldId);
        _instanceService.RegisterGroupWithInstance(created, group);
        return created;
    }
}
