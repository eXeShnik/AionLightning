using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// P1 siege subsystem (Java services.SiegeService — only the maps + getters + persisted ownership +
/// login/enter-world broadcast subset is ported here). The scheduler, capture flow, siege rewards,
/// assaults and Tiamaranta rifts are P2+ and not present. All client broadcasts are gated behind
/// <see cref="SiegeOptions.Enable"/> (default false) — see SiegeOptions for the rationale.
/// </summary>
public sealed class SiegeService(
    IDataManager dataManager,
    ISiegeDao siegeDao,
    LegionService legionService,
    IOptions<SiegeOptions> options,
    ILogger<SiegeService> log)
{
    private readonly Influence _influence = new();

    public IReadOnlyDictionary<int, FortressLocation> Fortresses => dataManager.Sieges.Fortresses;
    public IReadOnlyDictionary<int, ArtifactLocation> Artifacts => dataManager.Sieges.Artifacts;
    public IReadOnlyDictionary<int, OutpostLocation> Outposts => dataManager.Sieges.Outposts;
    public IReadOnlyDictionary<int, SourceLocation> Sources => dataManager.Sieges.Sources;
    public IReadOnlyDictionary<int, SiegeLocation> Locations => dataManager.Sieges.Locations;

    public FortressLocation? GetFortress(int id) => Fortresses.GetValueOrDefault(id);
    public ArtifactLocation? GetArtifact(int id) => Artifacts.GetValueOrDefault(id);
    public OutpostLocation? GetOutpost(int id) => Outposts.GetValueOrDefault(id);
    public SourceLocation? GetSource(int id) => Sources.GetValueOrDefault(id);
    public SiegeLocation? GetSiegeLocation(int id) => Locations.GetValueOrDefault(id);

    public IEnumerable<SiegeLocation> GetSiegeLocations(int worldId) =>
        Locations.Values.Where(l => l.WorldId == worldId);

    public bool IsVulnerable(int locationId) => GetSiegeLocation(locationId)?.IsVulnerable ?? false;

    public void SetUnderShield(int locationId, bool value) => GetSiegeLocation(locationId)?.SetUnderShield(value);

    public void SetCanTeleport(int locationId, bool value) => GetSiegeLocation(locationId)?.SetCanTeleport(value);

    /// <summary>Java ArtifactLocation.isStandAlone() — moved here since it reached into the
    /// SiegeService singleton from the model in Java.</summary>
    public bool IsStandaloneArtifact(int artifactLocationId) => !Fortresses.ContainsKey(artifactLocationId);

    /// <summary>Java ArtifactLocation.getOwningFortress().</summary>
    public FortressLocation? GetOwningFortress(int artifactLocationId) => GetFortress(artifactLocationId);

    /// <summary>Java OutpostLocation.isRouteSpawned() — moved here for the same reason as
    /// IsStandaloneArtifact/GetOwningFortress.</summary>
    public bool IsRouteSpawned(OutpostLocation outpost) =>
        outpost.FortressDependency.Any(fortressId =>
            Fortresses.TryGetValue(fortressId, out var f) && f.Race == outpost.LocationRace);

    public Influence GetInfluence() => _influence;

    public Model.Legion.Legion? GetLegion(int legionId) => legionService.GetById(legionId);

    /// <summary>Java getSecondsBeforeHourEnd().</summary>
    public int GetSecondsBeforeHourEnd()
    {
        var now = DateTime.Now;
        int elapsedSeconds = now.Minute * 60 + now.Second;
        return 3600 - elapsedSeconds;
    }

    /// <summary>
    /// note: Java derives this from the active Siege&lt;?&gt; scheduling/timer engine, which is P2+
    /// and not present in this port. Always 0 until that engine exists.
    /// </summary>
    public int GetRemainingSiegeTimeInSeconds(int siegeLocationId) => 0;

    /// <summary>Java setRace/setLegionId + DAOManager.getDAO(SiegeDAO.class).updateSiegeLocation —
    /// combined into one persisted ownership update.</summary>
    public async Task SetOwnerAsync(int locationId, SiegeRace race, int legionId, CancellationToken ct = default)
    {
        if (GetSiegeLocation(locationId) is not { } location) return;
        location.Race = race;
        location.LegionId = legionId;
        await siegeDao.UpsertAsync(locationId, race, legionId, ct);
    }

    /// <summary>Java cleanLegionId(int) — clears legion ownership when a legion is disbanded. Stops
    /// at the first match, matching the (likely unintentional) Java `break`.</summary>
    public void CleanLegionId(int legionId)
    {
        foreach (var loc in Locations.Values)
        {
            if (loc.LegionId != legionId) continue;
            loc.LegionId = 0;
            break;
        }
    }

    /// <summary>Java initSiegeLocations()'s DAOManager.getDAO(SiegeDAO.class).loadSiegeLocations call —
    /// loads persisted race/legion ownership from the DB, inserting default rows for any location not
    /// yet present. Called once at startup by SiegeServiceHostedService, after schema migration.</summary>
    public async Task LoadPersistedOwnershipAsync(CancellationToken ct = default)
    {
        if (Locations.Count == 0)
        {
            log.LogInformation("SiegeService: no siege locations loaded from static data, skipping ownership load.");
            return;
        }

        var rows = await siegeDao.LoadAllAsync(ct);
        var loaded = new HashSet<int>();
        foreach (var row in rows)
        {
            if (GetSiegeLocation(row.Id) is not { } loc) continue;
            loc.Race = row.Race;
            loc.LegionId = row.LegionId;
            loaded.Add(row.Id);
        }

        foreach (var loc in Locations.Values)
        {
            if (!loaded.Contains(loc.LocationId))
                await siegeDao.UpsertAsync(loc.LocationId, loc.Race, loc.LegionId, ct);
        }

        _influence.Recalculate(Locations.Values);
        log.LogInformation("SiegeService: loaded ownership for {Count} siege location(s)", Locations.Count);
    }

    // note: Java's spawnNpcs/deSpawnNpcs drive the siege spawn engine (SpawnGroup2 filtered by
    // SiegeSpawnTemplate race/modtype) and the SiegeNpc controller wiring — that spawn-template
    // subclass doesn't exist in this port yet. No-op stubs until P2 adds the siege spawn engine.
    public void SpawnNpcs(int siegeLocationId, SiegeRace race, SiegeModType type)
    {
    }

    public void DeSpawnNpcs(int siegeLocationId)
    {
    }

    /// <summary>Java onPlayerLogin(Player) — only the always-sent first part (siege ownership +
    /// influence ratio); the Tiamaranta rift-route SM_RIFT_ANNOUNCE part is P2+ (rift subsystem not
    /// ported). No-op while <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async ValueTask OnPlayerLoginAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        await conn.SendAsync(new SM_INFLUENCE_RATIO(this), ct);
        await conn.SendAsync(new SM_SIEGE_LOCATION_INFO(this, player), ct);
    }

    /// <summary>Java onEnterSiegeWorld(Player). No-op while <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async ValueTask OnEnterSiegeWorldAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        var worldLocations = Locations.Values.Where(l => l.WorldId == player.Position.WorldId).ToList();
        var worldArtifacts = Artifacts.Values.Where(a => a.WorldId == player.Position.WorldId).ToList();

        await conn.SendAsync(new SM_SHIELD_EFFECT(worldLocations), ct);
        await conn.SendAsync(new SM_ABYSS_ARTIFACT_INFO(worldArtifacts), ct);
    }
}
