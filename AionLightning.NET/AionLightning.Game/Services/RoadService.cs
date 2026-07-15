using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Road;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java model.road.Road + controllers.RoadController/observer.RoadObserver + services.RoadService
/// — invisible plane-crossing shortcuts between adjacent open-world zones (Verteron&lt;-&gt;Eltnen&lt;-&gt;
/// Heiron, Morheim&lt;-&gt;Altgard&lt;-&gt;Beluslan, and the contested Sarpan/Tiamaranta/Katalam/Danaria
/// border crossings), loaded by <see cref="DataHolders.RoadData"/> from data/static_data/roads/roads.xml —
/// that data ships in full with this dataset (14 roads), so no hardcoded fallback table is needed here
/// (unlike ShieldService's generator spawns, which aren't shipped).
///
/// note: despite the class's name suggesting fortress-ownership gating (by analogy with SiegeService's
/// abyss-road/outpost-route terminology), Java's actual RoadObserver.moved() gates purely on the
/// destination zone's WorldType and the crossing player's race — Verteron/Eltnen/Heiron are Elyos-only
/// (WorldType.ELYSEA), Morheim/Altgard/Beluslan are Asmodian-only (WorldType.ASMODAE), and every other
/// world type (Balaurea's contested war-zone crossings) is open to both races unconditionally. There is no
/// siege/fortress-ownership check anywhere in the Java source for this subsystem — verified against
/// controllers.RoadController/observer.RoadObserver/model.road.Road, none of which reference
/// SiegeService/FortressLocation. <see cref="IsRoadOpen"/> below mirrors that real condition instead of
/// inventing one that doesn't exist upstream.
///
/// note: the actual trigger (Java RoadObserver.moved(), fired whenever a player's straight-line movement
/// crosses the road's plane within its radius, via the knownlist "see" + per-tick movement-observer
/// framework) needs machinery this port doesn't have yet — the same collision/knownlist gap already
/// documented on Model.Siege.SiegeShield and ShieldService's geo-mesh note.
/// <see cref="TryCrossRoadAsync"/> is the on-demand equivalent (data load + gating + teleport, all fully
/// functional); it has no continuous movement-crossing caller yet, so nothing currently invokes it — a
/// future phase can wire it in once a per-tick position-crossing check (or its geo/knownlist replacement)
/// lands, without needing to revisit this class.
/// </summary>
public sealed class RoadService(IDataManager dataManager, TeleportService teleportService, ILogger<RoadService> log)
{
    public IReadOnlyList<RoadTemplate> Roads => dataManager.Roads.Templates;

    public RoadTemplate? GetRoad(int roadId) => Roads.FirstOrDefault(r => r.Id == roadId);

    /// <summary>Java RoadObserver.moved()'s per-race gate (the WorldType.ELYSEA/ASMODAE/else branches) —
    /// whether <paramref name="player"/> is currently allowed to cross <paramref name="road"/>.</summary>
    public bool IsRoadOpen(RoadTemplate road, Player player)
    {
        var worldType = dataManager.WorldMaps.GetTemplate(road.WorldId)?.WorldType;
        return worldType switch
        {
            "ELYSEA" => player.Race == Race.ELYOS,
            "ASMODAE" => player.Race == Race.ASMODIANS,
            _ => true,
        };
    }

    /// <summary>Overload for callers that only have the road id (e.g. a future portal/trigger handler).
    /// Returns false for an unknown road id.</summary>
    public bool IsRoadOpen(int roadId, Player player) => GetRoad(roadId) is { } road && IsRoadOpen(road, player);

    /// <summary>
    /// Java RoadObserver.moved()'s teleport half — moves <paramref name="player"/> to the road's exit
    /// point when <see cref="IsRoadOpen"/> allows it (Java teleported with TeleportAnimation.BEAM_ANIMATION;
    /// this port's <see cref="TeleportService.TeleportToAsync"/> already defaults its animation byte the
    /// same way every other non-instant teleport in this codebase does). Returns false (no-op, caller
    /// should fall through to whatever else it was doing) when the road id is unknown or the player's
    /// race is barred from it.
    /// </summary>
    public async ValueTask<bool> TryCrossRoadAsync(Player player, int roadId, CancellationToken ct = default)
    {
        if (GetRoad(roadId) is not { } road) return false;
        if (!IsRoadOpen(road, player)) return false;

        await teleportService.TeleportToAsync(player, road.ExitWorldId, 0,
            road.ExitPoint.X, road.ExitPoint.Y, road.ExitPoint.Z, ct: ct);

        log.LogDebug("RoadService: {Player} crossed road '{Road}' to world {World}", player.Name, road.Name, road.ExitWorldId);
        return true;
    }
}
