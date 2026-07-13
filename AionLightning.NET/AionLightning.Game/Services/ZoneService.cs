using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;
using GameWorld = AionLightning.Game.World.World;

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
    /// <summary>Max distance at which a move triggers a quest onAtDistance hook (tunable).</summary>
    private const float AtDistanceRange = 20f;

    private readonly IDataManager _dataManager;
    private readonly QuestEngineType _questEngine;
    private readonly InstanceService _instanceService;
    private readonly GameWorld _world;

    public ZoneService(IDataManager dataManager, QuestEngineType questEngine, InstanceService instanceService, GameWorld world)
    {
        _dataManager = dataManager;
        _questEngine = questEngine;
        _instanceService = instanceService;
        _world = world;
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
        // Quest onAtDistance: fire for any live registered NPC the player is now near. Runs every move
        // (skipped entirely when no quest registered an at-distance NPC). Handlers self-guard on quest var.
        var atDistance = _questEngine.AtDistanceNpcIds;
        if (atDistance.Count > 0)
        {
            var here = player.Position;
            foreach (var npc in _world.GetNpcsInScope(here))
                if (atDistance.Contains(npc.Template.NpcId) && npc.Position.DistanceTo(here) <= AtDistanceRange)
                    await _questEngine.OnAtDistanceAsync(new QuestEnv(npc, player, 0, 0), conn, ct);
        }

        // Quest onPassFlyingRing: fire for any fly ring of the player's world the player is now within
        // radius of (Java's center-proximity fallback). Skipped entirely when no fly-ring quest exists;
        // handlers self-guard on quest var so repeated fires while lingering are idempotent.
        if (_questEngine.HasFlyRingQuests)
        {
            var here = player.Position;
            foreach (var ring in _dataManager.FlyRings.GetRingsForWorld(here.WorldId))
            {
                float dx = here.X - ring.Cx, dy = here.Y - ring.Cy, dz = here.Z - ring.Cz;
                if (dx * dx + dy * dy + dz * dz <= ring.Radius * ring.Radius)
                    await _questEngine.OnPassFlyingRingAsync(player, ring.Name, conn, ct);
            }
        }

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

        // Zones the player was in but has now left (fire onLeaveZone before pruning the set).
        List<string>? left = null;
        foreach (var z in current)
            if (!insideNow.Contains(z)) (left ??= new List<string>()).Add(z);

        current.IntersectWith(insideNow);

        if (left is not null)
            foreach (var zoneName in left)
            {
                await _questEngine.OnLeaveZoneAsync(player, zoneName, conn, ct);
                if (player.Position.InstanceId != 0)
                    _instanceService.OnLeaveZone(player, zoneName);
            }

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
