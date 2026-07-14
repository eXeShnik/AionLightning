using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Housing subsystem (Java services.HousingService). P1 (load-at-startup + ownership maps + getters +
/// login owner-flags/broadcast) plus P2 world-spawn/rendering (<see cref="SpawnHouses"/>) are ported;
/// maintenance/fee scheduling and the studio-purchase flow live in MaintenanceTask and are still not
/// present here. Houses are tracked in this service's own dictionaries rather than the shared
/// <c>World</c> NPC/player store — <see cref="SpawnHouses"/> only ever mutates these dictionaries, so
/// enabling housing cannot regress the open-world NPC/combat spawn-and-broadcast loop. The
/// SM_HOUSE_OWNER_INFO/SM_HOUSE_RENDER/SM_HOUSE_UPDATE/SM_DELETE_HOUSE client broadcasts (and, per
/// <see cref="SpawnHouses"/>'s own gate, the spawn step itself) are gated behind
/// <see cref="HousingOptions.Enable"/> (default false) — see HousingOptions for the rationale.
/// </summary>
public sealed class HousingService(
    IHouseDao houseDao,
    IPlayerRegisteredItemsDao registeredItemsDao,
    IHouseScriptsDao houseScriptsDao,
    IDataManager dataManager,
    IOptions<HousingOptions> options,
    ILogger<HousingService> log)
{
    // Keyed by HouseAddress.Id (Java customHouses).
    private readonly Dictionary<int, House> _customHouses = new();
    // Keyed by owning player's objectId (Java studios).
    private readonly Dictionary<int, House> _studios = new();
    private readonly object _lock = new();

    /// <summary>Java HousingService()'s constructor-time DAOManager.getDAO(HousesDAO.class).loadHouses
    /// calls — loads every persisted house row and splits it into the studio/custom-house maps by
    /// building type (PERSONAL_INS = studio, keyed by owner; everything else = custom house, keyed by
    /// address). Called once at startup by HousingServiceHostedService.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var houses = await houseDao.LoadAllAsync(ct);

        lock (_lock)
        {
            _customHouses.Clear();
            _studios.Clear();
            foreach (var house in houses)
            {
                if (IsStudioBuilding(house.BuildingId))
                    _studios[house.PlayerObjectId] = house;
                else
                    _customHouses[house.Address] = house;
            }
        }

        log.LogInformation("HousingService: loaded {Custom} custom house(s), {Studios} studio(s)",
            _customHouses.Count, _studios.Count);
    }

    /// <summary>
    /// Java services.HousingService.spawnHouses(worldId, instanceId, registeredId) — collapsed to a single
    /// startup-time sweep across every housing land instead of Java's per-world-load invocation (this port
    /// has no per-world "spawn on load" hook to call it from), and always into the open world
    /// (instanceId 0; nothing here places a house inside a dungeon instance). For every land's addresses
    /// (skipping studio lands — studios are per-owner, spawned separately from <see cref="_studios"/>),
    /// materializes a missing/never-persisted custom house the same way Java does (a fresh, unowned,
    /// NoSale house keyed by address — Java's <c>PersistentState.NEW</c>, only ever written to the DB once
    /// acquired), then positions it and loads its default building-part decorations.
    ///
    /// No-op entirely (not even the missing-house materialization) unless <see cref="HousingOptions.Enable"/>
    /// is true: houses are new SM_HOUSE_* traffic on unverified 4.5-era opcodes, so this whole subsystem
    /// stays off the byte-verified enter-world path until enabled. Called once at startup by
    /// <see cref="HousingServiceHostedService"/>, after <see cref="LoadAsync"/> and before
    /// <see cref="HousingBidService.LoadAsync"/> (so bid data resolves against a fully-populated house set).
    /// </summary>
    public void SpawnHouses()
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("HousingService: world-spawn disabled (GameServer:Housing:Enable=false) — houses not spawned.");
            return;
        }

        int customSpawned = 0, studiosSpawned = 0, skipped = 0;

        lock (_lock)
        {
            foreach (var land in dataManager.Housing.Lands)
            {
                var defaultBuilding = land.DefaultBuilding;
                if (defaultBuilding is null || defaultBuilding.Type == BuildingType.PERSONAL_INS)
                    continue; // studio lands are handled via the _studios dictionary below.

                foreach (var address in land.Addresses)
                {
                    if (!_customHouses.TryGetValue(address.Id, out var house))
                    {
                        house = new House
                        {
                            Id = ObjectIdFactory.Next(),
                            Address = address.Id,
                            BuildingId = defaultBuilding.Id,
                            Status = HouseStatus.NoSale,
                        };
                        _customHouses[address.Id] = house;
                    }

                    var building = dataManager.Housing.GetBuilding(house.BuildingId) ?? defaultBuilding;
                    SpawnHouse(house, building, address);
                    customSpawned++;
                }
            }

            foreach (var studio in _studios.Values)
            {
                var address = dataManager.Housing.GetAddress(studio.Address);
                var building = dataManager.Housing.GetBuilding(studio.BuildingId);
                if (address is null || building is null) { skipped++; continue; }

                SpawnHouse(studio, building, address);
                studiosSpawned++;
            }
        }

        log.LogInformation("HousingService: spawned {Custom} custom house(s), {Studios} studio(s) ({Skipped} skipped)",
            customSpawned, studiosSpawned, skipped);
    }

    private static void SpawnHouse(House house, Building building, HouseAddress address)
    {
        house.FixBuildingStates();
        house.Position = new Position(address.X, address.Y, address.Z, 0, address.MapId);
        house.Registry.LoadDefaultParts(building);
    }

    /// <summary>
    /// Java House.spawn()'s <c>PlayerRegisteredItemsDAO.loadRegistry(playerObjectId)</c> call, collapsed
    /// (like <see cref="SpawnHouses"/>) into a single startup-time sweep over every owned house instead of
    /// Java's per-house-spawn invocation. Resolves each DB row into a <see cref="HouseObject"/> or, for the
    /// "DECOR" sentinel rows, a custom <see cref="HouseDecoration"/> (re-activating it via
    /// <see cref="HouseRegistry.SetPartInUse"/> when it was in use at last save) — see
    /// <see cref="Dao.PlayerRegisteredItemRow"/>'s doc for the shared-table layout.
    /// No-op unless <see cref="HousingOptions.Enable"/> is true. Called once at startup by
    /// <see cref="HousingServiceHostedService"/>, after <see cref="SpawnHouses"/>.
    /// </summary>
    public async Task LoadRegisteredItemsAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        List<House> owned;
        lock (_lock)
        {
            owned = _customHouses.Values.Concat(_studios.Values)
                .Where(h => h.IsOwned && h.Status is HouseStatus.Active or HouseStatus.SellWait)
                .ToList();
        }

        int rowsLoaded = 0;
        foreach (var house in owned)
        {
            var rows = await registeredItemsDao.LoadByPlayerAsync(house.PlayerObjectId, ct);
            foreach (var row in rows)
                ApplyRegisteredItemRow(house, row);
            rowsLoaded += rows.Count;
        }

        log.LogInformation("HousingService: loaded {Rows} registered item row(s) across {Houses} owned house(s)",
            rowsLoaded, owned.Count);
    }

    /// <summary>
    /// Java House.spawn()'s <c>HouseScriptsDAO.getPlayerScripts(houseId)</c> call, collapsed (like
    /// <see cref="SpawnHouses"/>/<see cref="LoadRegisteredItemsAsync"/>) into a single startup-time sweep
    /// over every house instead of Java's per-house-spawn invocation. Unlike
    /// <see cref="LoadRegisteredItemsAsync"/>, this runs for every house regardless of ownership — Java
    /// loads scripts unconditionally as the very first line of house.spawn(), before its ownership check.
    /// No-op unless <see cref="HousingOptions.Enable"/> is true. Called once at startup by
    /// <see cref="HousingServiceHostedService"/>, after <see cref="SpawnHouses"/>.
    /// </summary>
    public async Task LoadHouseScriptsAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        List<House> houses;
        lock (_lock)
        {
            houses = _customHouses.Values.Concat(_studios.Values).ToList();
        }

        int rowsLoaded = 0;
        foreach (var house in houses)
        {
            var rows = await houseScriptsDao.LoadAsync(house.Id, ct);
            foreach (var (position, script) in rows)
                house.Registry.Scripts.LoadPersisted(position, script);
            rowsLoaded += rows.Count;
        }

        log.LogInformation("HousingService: loaded {Rows} house script row(s) across {Houses} house(s)",
            rowsLoaded, houses.Count);
    }

    private void ApplyRegisteredItemRow(House house, PlayerRegisteredItemRow row)
    {
        if (string.Equals(row.Area, "DECOR", StringComparison.OrdinalIgnoreCase))
        {
            var part = dataManager.HouseParts.GetPartById(row.ItemId);
            if (part is null) return; // note: house_parts.xml missing this id — can't resolve its slot type.

            var decor = new HouseDecoration(row.ItemId, part.Type, row.Floor, row.ItemUniqueId)
            {
                IsUsed = row.OwnerUseCount > 0, // Java reuses owner_use_count as the "isUsed" bit for DECOR rows
                PersistentState = PersistentState.Updated,
            };
            house.Registry.PutCustomPart(decor);
            if (decor.IsUsed)
                house.Registry.SetPartInUse(decor, decor.Floor);
        }
        else
        {
            var template = dataManager.HousingObjects.GetTemplateById(row.ItemId);
            var obj = new HouseObject(house, row.ItemUniqueId, row.ItemId, template)
            {
                X = row.X,
                Y = row.Y,
                Z = row.Z,
                Heading = (byte)row.H,
                Color = row.Color,
                ColorExpireEnd = row.ColorExpires,
                OwnerUsedCount = row.OwnerUseCount,
                VisitorUsedCount = row.VisitorUseCount,
                ExpireEnd = template is { UseDays: > 0 } ? row.ExpireTime ?? 0 : 0,
                PersistentState = PersistentState.Updated,
            };
            house.Registry.PutObject(obj);
        }
    }

    /// <summary>Locates a placed/registered house object by its registry objectId across every house this
    /// service tracks. Java resolves this via World.findVisibleObject (HouseObject extends VisibleObject);
    /// this port never spawns HouseObjects into the shared World store (see <see cref="SpawnHouses"/>'s own
    /// doc on why houses themselves stay out of it too), so CM_USE_HOUSE_OBJECT/CM_RELEASE_OBJECT scan the
    /// housing dictionaries directly instead.</summary>
    public HouseObject? FindHouseObject(int objectId)
    {
        lock (_lock)
        {
            foreach (var house in _customHouses.Values.Concat(_studios.Values))
                if (house.Registry.GetObjectByObjId(objectId) is { } obj)
                    return obj;
        }
        return null;
    }

    /// <summary>Houses (custom + studios) currently spawned in the same world/instance scope as
    /// <paramref name="scope"/> — used to introduce them to a player entering that scope (see
    /// CM_LEVEL_READY).</summary>
    public List<House> GetHousesInScope(Position scope)
    {
        lock (_lock)
        {
            return _customHouses.Values.Concat(_studios.Values)
                .Where(h => h.Position is { } pos && pos.SameScope(scope))
                .ToList();
        }
    }

    private bool IsStudioBuilding(int buildingId) =>
        dataManager.Housing.GetBuilding(buildingId)?.Type == BuildingType.PERSONAL_INS;

    /// <summary>Java searchPlayerHouses(int).</summary>
    public List<House> SearchPlayerHouses(int playerObjId)
    {
        lock (_lock)
        {
            if (_studios.TryGetValue(playerObjId, out var studio))
                return [studio];

            return _customHouses.Values.Where(h => h.PlayerObjectId == playerObjId).ToList();
        }
    }

    /// <summary>Java getPlayerAddress(int).</summary>
    public int GetPlayerAddress(int playerId)
    {
        lock (_lock)
        {
            if (_studios.TryGetValue(playerId, out var studio))
                return studio.Address;

            foreach (var house in _customHouses.Values)
            {
                if (house.Status == HouseStatus.Inactive)
                    continue;
                if (house.PlayerObjectId == playerId && house.Status is HouseStatus.Active or HouseStatus.SellWait)
                    return house.Address;
            }
        }
        return 0;
    }

    /// <summary>Java getHouseByAddress(int).</summary>
    public House? GetHouseByAddress(int address)
    {
        lock (_lock)
        {
            return _customHouses.Values.FirstOrDefault(h => h.Address == address);
        }
    }

    /// <summary>Java House.getLevelRestrict() — the minimum level to enter/bid, taken from the owning
    /// land's sale options (10 when the land can't be resolved, matching Java's fallback).</summary>
    public int GetLevelRestrict(House house) =>
        dataManager.Housing.GetLandByAddress(house.Address)?.SaleOptions.MinLevel ?? 10;

    /// <summary>Java getCustomHouses() — a snapshot of every custom house tracked by this service,
    /// regardless of ownership. Used by HousingBidService to correlate persisted bids/auto-fill auctions
    /// to houses at startup.</summary>
    public List<House> GetCustomHouses()
    {
        lock (_lock)
        {
            return _customHouses.Values.ToList();
        }
    }

    /// <summary>Java getPlayerStudio(int).</summary>
    public House? GetPlayerStudio(int playerId)
    {
        lock (_lock)
        {
            return _studios.GetValueOrDefault(playerId);
        }
    }

    /// <summary>Java onPlayerLogin(Player) — owner-flags computation always runs; only the
    /// SM_HOUSE_OWNER_INFO broadcast is gated behind <see cref="HousingOptions.Enable"/>. The Java
    /// method's trailing SM_FRIEND_LIST/SM_MARK_FRIENDLIST sends are handled elsewhere in this port's
    /// enter-world flow, not here.</summary>
    public async ValueTask OnPlayerLoginAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        player.Houses.Clear();
        player.Houses.AddRange(SearchPlayerHouses(player.ObjectId));

        var activeHouse = player.ActiveHouse;
        byte buildingState = (byte)PlayerHouseOwnerFlags.BuyStudioAllowed;
        if (activeHouse is null)
        {
            int questId = player.Race == Race.ELYOS ? 18802 : 28802;
            if (player.Quests.Get(questId)?.Status == QuestStatus.COMPLETE)
                buildingState |= (byte)PlayerHouseOwnerFlags.BiddingAllowed;
        }
        else
        {
            buildingState = activeHouse.Status == HouseStatus.SellWait
                ? (byte)PlayerHouseOwnerFlags.SellingHouse
                : (byte)PlayerHouseOwnerFlags.HouseOwner;
        }
        player.BuildingOwnerState = buildingState;

        if (!options.Value.Enable) return;

        bool isStudio = activeHouse is not null && IsStudioBuilding(activeHouse.BuildingId);
        int weeksUntilDue = MaintenanceTask.ComputeWeeksUntilDue(activeHouse, isStudio, MaintenanceTask.MaintenanceCron);
        await conn.SendAsync(new SM_HOUSE_OWNER_INFO(player, activeHouse, weeksUntilDue), ct);
    }
}
