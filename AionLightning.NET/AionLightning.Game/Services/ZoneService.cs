using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Services;

/// <summary>
/// Tracks each player's current zone-region membership and fires the quest onEnterZone hook on
/// transitions (Java <c>world.zone.ZoneInstance.onEnter</c> -&gt;
/// <c>PlayerController.onEnterZone</c> -&gt; <c>QuestEngine.onEnterZone</c>). Membership is
/// recomputed from scratch against the player's current world's zone regions every time this is
/// called — callers (the CM_MOVE path) only invoke it on an actual position change, so this stays
/// a cheap per-movement-tick check rather than a background poll.
/// </summary>
public sealed class ZoneService
{
    private readonly IDataManager _dataManager;
    private readonly QuestEngineType _questEngine;
    private readonly InstanceService _instanceService;

    public ZoneService(IDataManager dataManager, QuestEngineType questEngine, InstanceService instanceService)
    {
        _dataManager = dataManager;
        _questEngine = questEngine;
        _instanceService = instanceService;
    }

    /// <summary>
    /// Recomputes the zone regions <paramref name="player"/> is now inside for their current world
    /// (point-in-polygon/cylinder/sphere membership, Java <c>ZoneInstance.isInsideCordinate</c>),
    /// diffs against <see cref="Player.CurrentZones"/>, and fires onEnterZone for newly-entered
    /// regions. Leaving a region only updates the tracked set — Java's onLeaveZone quest hook has
    /// no ported consumer yet, so it is intentionally not dispatched here (see task scope notes).
    /// </summary>
    public async ValueTask UpdateZonesAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var regions = _dataManager.Zones.GetRegionsForWorld(player.Position.WorldId);
        var current = player.CurrentZones;

        if (regions.Count == 0)
        {
            current.Clear();
            return;
        }

        var pos = player.Position;
        var insideNow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string>? entered = null;

        foreach (var region in regions)
        {
            if (!region.IsInside(pos.X, pos.Y, pos.Z)) continue;

            insideNow.Add(region.Name);
            if (!current.Contains(region.Name))
                (entered ??= new List<string>()).Add(region.Name);
        }

        current.IntersectWith(insideNow);
        if (entered is null) return;

        foreach (var zoneName in entered)
        {
            current.Add(zoneName);
            await _questEngine.OnEnterZoneAsync(player, zoneName, conn, ct);
            if (player.Position.InstanceId != 0)
                _instanceService.OnEnterZone(player, zoneName);
        }
    }
}
