using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Controllers;

/// <summary>
/// Java controllers.HouseController. Java has one controller instance per House (an "observed" map of
/// currently-visible players lives on each instance); this port instead keeps a single DI-singleton
/// controller whose observed-set dictionary is keyed by House.Id, matching how this codebase already
/// drives NPCs/gatherables — one service over a shared dictionary — rather than a per-object controller
/// hierarchy (see HousingService.SpawnHouses's doc comment). Every send is gated behind
/// <see cref="HousingOptions.Enable"/>.
/// </summary>
public sealed class HouseController(
    IDataManager dataManager,
    LegionService legionService,
    PlayerConnectionRegistry connRegistry,
    GameWorld world,
    TeleportService teleport,
    IOptions<HousingOptions> options)
{
    private readonly Dictionary<int, HashSet<int>> _observed = new();
    private readonly object _lock = new();

    /// <summary>
    /// Java HouseController.see(VisibleObject) — introduces a house to a player who just came into its
    /// visibility range: SM_HOUSE_RENDER for an open-world house, SM_HOUSE_UPDATE for an instanced one.
    /// note: Java's trailing spawnObjects() call (player-placed furniture) has nothing to spawn in this
    /// phase — no HouseObject/custom-decoration system is ported yet.
    /// </summary>
    public async ValueTask SeeAsync(House house, Player observer, GsClientConnection conn, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;
        var building = dataManager.Housing.GetBuilding(house.BuildingId);
        if (building is null) return;

        lock (_lock)
        {
            if (!_observed.TryGetValue(house.Id, out var set))
                _observed[house.Id] = set = new HashSet<int>();
            set.Add(observer.ObjectId);
        }

        house.FixBuildingStates();
        house.NormalizePermissions(building.Type);
        var legion = legionService.GetByPlayerId(house.PlayerObjectId);

        bool inInstance = (house.Position?.InstanceId ?? 0) != 0;
        AionServerPacket packet = inInstance
            ? new SM_HOUSE_UPDATE(house, building, legion)
            : new SM_HOUSE_RENDER(house, building, legion);
        try { await conn.SendAsync(packet, ct); } catch { }
    }

    /// <summary>Java HouseController.notSee — stops tracking the observer; on an out-of-range departure
    /// from an open-world (non-instanced) house, tells the client to despawn it.</summary>
    public async ValueTask NotSeeAsync(House house, Player observer, GsClientConnection conn, bool isOutOfRange, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        lock (_lock)
        {
            if (_observed.TryGetValue(house.Id, out var set))
                set.Remove(observer.ObjectId);
        }

        bool inInstance = (house.Position?.InstanceId ?? 0) != 0;
        if (isOutOfRange && !inInstance)
            try { await conn.SendAsync(new SM_DELETE_HOUSE(house.Address), ct); } catch { }
    }

    /// <summary>Java HouseController.broadcastAppearance — re-renders the house for everyone currently
    /// observing it (used after an appearance change, e.g. a building switch).</summary>
    public async Task BroadcastAppearanceAsync(House house, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;
        var building = dataManager.Housing.GetBuilding(house.BuildingId);
        if (building is null) return;

        List<int> observers;
        lock (_lock)
            observers = _observed.TryGetValue(house.Id, out var set) ? set.ToList() : new List<int>();
        if (observers.Count == 0) return;

        house.FixBuildingStates();
        house.NormalizePermissions(building.Type);
        var legion = legionService.GetByPlayerId(house.PlayerObjectId);
        var packet = new SM_HOUSE_RENDER(house, building, legion);

        foreach (var playerId in observers)
            if (connRegistry.Get(playerId) is { } peerConn)
                try { await peerConn.SendAsync(packet, ct); } catch { }
    }

    /// <summary>
    /// Java HouseController.kickVisitors — evicts every non-owner near the house to a safe exit.
    /// note: Java enumerates visitors via the house's dedicated housing zone (matched by house name); this
    /// port has no per-house zone-region data, so it instead evicts everyone currently tracked as
    /// "observing" the house (i.e. within its render/visibility range) — a superset-safe approximation.
    /// The friend exemption (kickFriends=false normally spares the kicker's friends) is not applied either,
    /// since no FriendList model exists on <see cref="Player"/> yet — every non-owner visitor is evicted.
    /// </summary>
    public async Task KickVisitorsAsync(House house, bool onSettingsChange, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        List<int> observers;
        lock (_lock)
            observers = _observed.TryGetValue(house.Id, out var set) ? set.ToList() : new List<int>();
        if (observers.Count == 0) return;

        var address = dataManager.Housing.GetAddress(house.Address);
        foreach (var playerId in observers)
        {
            if (playerId == house.PlayerObjectId) continue;
            var visitor = world.GetPlayerByObjectId(playerId);
            if (visitor is null) continue;
            await MoveOutsideAsync(visitor, house, address, ct);
        }
    }

    /// <summary>
    /// Java HouseController.moveOutside. note: Java's non-exit-address branch teleports relative to the
    /// house's sign NPC heading; this port doesn't spawn house sign/manager/teleport NPCs yet (see
    /// HousingService.SpawnHouse), so addresses without exit coordinates fall back to the house's own
    /// spawn position instead. The onSettingsChange/STR_MSG_HOUSING_* system messages Java sends alongside
    /// the teleport are not reproduced (string ids not confirmed against a live client — see class doc).
    /// Public so CM_HOUSE_TELEPORT_BACK can reuse it for the single-player "teleport back out" flow.
    /// </summary>
    public async Task MoveOutsideAsync(Player visitor, House house, HouseAddress? address, CancellationToken ct)
    {
        byte heading = (byte)visitor.Position.Heading;

        if (address is { ExitMapId: { } exitMap, ExitX: { } ex, ExitY: { } ey, ExitZ: { } ez })
            await teleport.TeleportToAsync(visitor, exitMap, 0, ex, ey, ez, heading, portAnimation: 0, ct);
        else if (house.Position is { } pos)
            await teleport.TeleportToAsync(visitor, pos.WorldId, pos.InstanceId, pos.X, pos.Y, pos.Z, heading, portAnimation: 0, ct);
    }
}
