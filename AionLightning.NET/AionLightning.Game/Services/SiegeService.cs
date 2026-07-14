using System.Collections.Concurrent;
using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using GameWorld = AionLightning.Game.World.World;
using SiegeInstance = AionLightning.Game.Services.Siege.Siege;

namespace AionLightning.Game.Services;

/// <summary>
/// Siege subsystem (Java services.SiegeService). P1 covered the location maps + getters + persisted
/// ownership + login/enter-world broadcast subset. This phase (P2) adds the lifecycle engine: scheduling
/// (CronService, ported from Java's SiegeStartRunnable/siege_schedule.xml), starting/stopping active
/// sieges (Services.Siege.Siege and its Fortress/Source/Outpost/Artifact subclasses), the siege-NPC
/// registry the boss-death/damage event handlers and Siege.InitSiegeBoss consult, and the next-state
/// broadcast used by SM_SIEGE_LOCATION_INFO/SM_FORTRESS_STATUS. All client broadcasts — and, per this
/// phase, all cron arming/spawning — remain gated behind <see cref="SiegeOptions.Enable"/> (default
/// false); see SiegeOptions for the rationale.
/// </summary>
public sealed class SiegeService(
    IDataManager dataManager,
    ISiegeDao siegeDao,
    LegionService legionService,
    GameWorld world,
    IPlayerDao playerDao,
    PlayerConnectionRegistry connRegistry,
    CronService cronService,
    MailFormatter mailFormatter,
    SpawnService spawnService,
    IOptions<SiegeOptions> options,
    IOptions<SiegeScheduleOptions> scheduleOptions,
    ILogger<SiegeService> log)
{
    // Java SiegeService.SIEGE_LOCATION_STATUS_BROADCAST_SCHEDULE — hourly fortress-status resync.
    private const string StatusBroadcastCron = "0 0 * ? * *";

    private readonly Influence _influence = new();
    private readonly ConcurrentDictionary<int, SiegeInstance> _activeSieges = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, SiegeNpc>> _siegeNpcsByLocation = new();
    private readonly ConcurrentDictionary<int, SiegeNpc> _siegeNpcsByObjectId = new();
    private readonly Dictionary<int, List<string>> _siegeSchedule = new();

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
    /// Java getRemainingSiegeTimeInSeconds(int) — kept faithful to Java's own logic even though it reads
    /// oddly: for an already-started, non-endless siege it measures duration-from-now rather than
    /// duration-from-StartTime (Java's own "TODO: Check if it's valid" comment on this method), so it
    /// effectively always returns the full configured duration once a siege is running.
    /// </summary>
    public int GetRemainingSiegeTimeInSeconds(int siegeLocationId)
    {
        if (GetSiege(siegeLocationId) is not { } siege || siege.Finished) return 0;
        if (!siege.Started) return siege.LocationBase.SiegeDuration;
        if (siege.LocationBase.SiegeDuration == -1) return -1;

        var target = DateTime.Now.AddSeconds(siege.LocationBase.SiegeDuration);
        int result = (int)(target - DateTime.Now).TotalSeconds;
        return result > 0 ? result : 0;
    }

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

    /// <summary>
    /// Java SiegeService.spawnNpcs(int, SiegeRace, SiegeModType) — spawns every static siege-spawn
    /// template for this location whose race/mod matches the currently-requested state (the call sites
    /// in Services.Siege.* already pass the location's live race and PEACE/SIEGE mod, mirroring Java's
    /// own call sites), tags each as a <see cref="SiegeNpc"/>, registers it, and broadcasts SM_NPC_INFO
    /// to whoever is already in scope. A no-op while <see cref="SiegeOptions.Enable"/> is false, matching
    /// Java's SiegeConfig.SIEGE_ENABLED guard inside VisibleObjectSpawner.spawnSiegeNpc.
    /// </summary>
    public void SpawnNpcs(int siegeLocationId, SiegeRace race, SiegeModType type)
    {
        if (!options.Value.Enable) return;

        var siegeSpawns = dataManager.SiegeSpawns.GetSiegeSpawnsBySiegeId(siegeLocationId);
        if (siegeSpawns.Count == 0) return;

        var spawned = new List<SiegeNpc>();
        foreach (var template in siegeSpawns)
        {
            if (template.SiegeRace != race || template.SiegeModType != type) continue;

            if (spawnService.SpawnSiegeNpc(template) is not { } siegeNpc) continue;
            RegisterSiegeNpc(siegeNpc);
            spawned.Add(siegeNpc);
        }

        if (spawned.Count == 0) return;

        _ = Task.Run(async () =>
        {
            foreach (var siegeNpc in spawned)
            {
                var infoPacket = new SM_NPC_INFO(siegeNpc.Npc);
                var scope = siegeNpc.Npc.Position;
                foreach (var conn in connRegistry.GetAll())
                    if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                        try { await conn.SendAsync(infoPacket); } catch { }
            }
        });
    }

    /// <summary>Java SiegeService.deSpawnNpcs(int) — removes every currently-registered siege NPC for
    /// this location from the world and the registry, broadcasting SM_DELETE to whoever can see them.</summary>
    public void DeSpawnNpcs(int siegeLocationId)
    {
        var localNpcs = GetLocalSiegeNpcs(siegeLocationId).ToList();
        if (localNpcs.Count == 0) return;

        foreach (var siegeNpc in localNpcs)
        {
            world.Remove(siegeNpc.Npc);
            UnregisterSiegeNpc(siegeNpc);
        }

        _ = Task.Run(async () =>
        {
            foreach (var siegeNpc in localNpcs)
            {
                var deletePacket = new SM_DELETE(siegeNpc.Npc.ObjectId);
                var scope = siegeNpc.Npc.Position;
                foreach (var conn in connRegistry.GetAll())
                    if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                        try { await conn.SendAsync(deletePacket); } catch { }
            }
        });
    }

    // --- Siege-NPC registry (Java World.getLocalSiegeNpcs) ---
    // Populated by SpawnNpcs/emptied by DeSpawnNpcs above. Boss identification (Siege.InitSiegeBoss
    // looking for the single IsBoss==true entry) still always throws SiegeException today: Java flags the
    // boss via NpcTemplate.getAbyssNpcType()==BOSS, and that field doesn't exist in this port's NPC static
    // data yet (see SiegeNpc.IsBoss/Siege.InitSiegeBoss doc comments) — a separate, already-documented gap
    // from the spawn engine itself.

    public IEnumerable<SiegeNpc> GetLocalSiegeNpcs(int locationId) =>
        _siegeNpcsByLocation.TryGetValue(locationId, out var bucket) ? bucket.Values : [];

    public SiegeNpc? GetSiegeNpc(Npc npc) => _siegeNpcsByObjectId.GetValueOrDefault(npc.ObjectId);

    public void RegisterSiegeNpc(SiegeNpc npc)
    {
        _siegeNpcsByObjectId[npc.Npc.ObjectId] = npc;
        _siegeNpcsByLocation.GetOrAdd(npc.SiegeId, _ => new ConcurrentDictionary<int, SiegeNpc>())[npc.Npc.ObjectId] = npc;
    }

    public void UnregisterSiegeNpc(SiegeNpc npc)
    {
        _siegeNpcsByObjectId.TryRemove(npc.Npc.ObjectId, out _);
        if (_siegeNpcsByLocation.TryGetValue(npc.SiegeId, out var bucket))
            bucket.TryRemove(npc.Npc.ObjectId, out _);
    }

    // --- Active siege lifecycle (Java SiegeService.startSiege/stopSiege/getSiege) ---

    public SiegeInstance? GetSiege(int siegeLocationId) => _activeSieges.GetValueOrDefault(siegeLocationId);

    public bool IsSiegeInProgress(int siegeLocationId) => _activeSieges.ContainsKey(siegeLocationId);

    /// <summary>Java startSiege(int) — constructs and starts the siege for a location, then (unless the
    /// siege is endless) schedules the automatic stop after its configured duration.</summary>
    public async Task StartSiegeAsync(int siegeLocationId, CancellationToken ct = default)
    {
        var siege = NewSiege(siegeLocationId);
        if (!_activeSieges.TryAdd(siegeLocationId, siege))
        {
            log.LogError("Attempt to start siege twice for siege location: {Id}", siegeLocationId);
            return;
        }

        await siege.StartSiegeAsync(ct);

        if (siege.IsEndless) return;

        int durationSeconds = siege.LocationBase.SiegeDuration;
        _ = ScheduleAutoStopAsync(siegeLocationId, durationSeconds, ct);
    }

    private async Task ScheduleAutoStopAsync(int siegeLocationId, int durationSeconds, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, durationSeconds)), ct);
            await StopSiegeAsync(siegeLocationId, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Error auto-stopping siege for location {Id}", siegeLocationId);
        }
    }

    /// <summary>Java stopSiege(int) — a no-op if the siege already ended (captured earlier, or a
    /// concurrent boss-death/timer race already stopped it).</summary>
    public async Task StopSiegeAsync(int siegeLocationId, CancellationToken ct = default)
    {
        if (!_activeSieges.TryRemove(siegeLocationId, out var siege) || siege.Finished)
            return;

        await siege.StopSiegeAsync(ct);
    }

    private SiegeInstance NewSiege(int siegeLocationId)
    {
        if (Fortresses.TryGetValue(siegeLocationId, out var fortress))
            return new Siege.FortressSiege(fortress, this, log, world, playerDao, mailFormatter, options.Value.MedalRate);
        if (Sources.TryGetValue(siegeLocationId, out var source))
            return new Siege.SourceSiege(source, this, log, mailFormatter, options.Value.MedalRate);
        if (Outposts.TryGetValue(siegeLocationId, out var outpost))
            return new Siege.OutpostSiege(outpost, this, log);
        if (Artifacts.TryGetValue(siegeLocationId, out var artifact))
            return new Siege.ArtifactSiege(artifact, this, log);

        throw new Siege.SiegeException($"Unknown siege handler for siege location: {siegeLocationId}");
    }

    /// <summary>
    /// Java initSieges()'s cron-scheduling half (SiegeStartRunnable per fortress/source siege time,
    /// standalone-artifact siege start, race-protector outpost cron, hourly status broadcast) plus
    /// updateFortressNextState(). Called once at startup by SiegeServiceHostedService, after
    /// LoadPersistedOwnershipAsync. Only arms cron / starts anything when
    /// <see cref="SiegeOptions.Enable"/> is true — otherwise the whole engine stays fully constructed
    /// but dormant.
    /// </summary>
    public async Task ScheduleSieges(CancellationToken ct = default)
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("SiegeService: siege engine disabled (GameServer:Siege:Enable=false) — cron not armed.");
            return;
        }

        foreach (var (locationId, cronExpressions) in scheduleOptions.Value.Fortresses)
            await ScheduleLocationAsync(locationId, cronExpressions);
        foreach (var (locationId, cronExpressions) in scheduleOptions.Value.Sources)
            await ScheduleLocationAsync(locationId, cronExpressions);

        await cronService.Schedule(() => FireAndForget(StartOutpostSiegesAsync(), "outpost race-protector siege start"),
            options.Value.RaceProtectorSpawnCron, longRunningTask: true);

        foreach (var artifact in Artifacts.Values.Where(a => IsStandaloneArtifact(a.LocationId)))
        {
            try
            {
                await StartSiegeAsync(artifact.LocationId, ct);
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to start siege for standalone artifact {Id}", artifact.LocationId);
            }
        }

        UpdateFortressNextState();

        await cronService.Schedule(() => FireAndForget(BroadcastFortressStatusAsync(), "hourly fortress status broadcast"),
            StatusBroadcastCron, longRunningTask: true);
    }

    private async Task ScheduleLocationAsync(int locationId, List<string> cronExpressions)
    {
        _siegeSchedule[locationId] = cronExpressions;
        foreach (var cron in cronExpressions)
            await cronService.Schedule(() => FireAndForget(StartSiegeAsync(locationId), $"siege start for location {locationId}"), cron, longRunningTask: true);
    }

    private async Task StartOutpostSiegesAsync()
    {
        foreach (var outpost in Outposts.Values.Where(o => o.IsSiegeAllowed))
        {
            try
            {
                await StartSiegeAsync(outpost.LocationId);
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to start outpost siege {Id}", outpost.LocationId);
            }
        }
    }

    private async Task BroadcastFortressStatusAsync()
    {
        UpdateFortressNextState();
        if (!options.Value.Enable) return; // defense in depth — this cron is only ever armed while enabled

        var fortressInfoBefore = Fortresses.Values.Select(f => new SM_FORTRESS_INFO(f.LocationId, false)).ToList();
        var status = new SM_FORTRESS_STATUS(this);
        var fortressInfoAfter = Fortresses.Values.Select(f => new SM_FORTRESS_INFO(f.LocationId, true)).ToList();

        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is null) continue;
            try
            {
                foreach (var p in fortressInfoBefore) await conn.SendAsync(p);
                await conn.SendAsync(status);
                foreach (var p in fortressInfoAfter) await conn.SendAsync(p);
            }
            catch
            {
                // best-effort broadcast — a dropped connection shouldn't abort the sweep for everyone else
            }
        }
    }

    /// <summary>
    /// Fire-and-forget helper for CronService.Schedule (which only accepts a synchronous <see cref="Action"/>,
    /// mirroring Java's Runnable dispatched by Quartz's own thread pool). Faults are logged rather than
    /// silently dropped or allowed to crash the scheduler thread.
    /// </summary>
    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "Unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);

    /// <summary>
    /// Java updateFortressNextState() — Java introspected the Quartz JobDetail/Trigger set to find each
    /// fortress/source's next scheduled siege start; this port has the same information locally (the
    /// cron expressions it just armed in <see cref="ScheduleSieges"/>) and evaluates them directly via
    /// Quartz.NET's <see cref="CronExpression"/> instead of round-tripping through CronService.
    /// </summary>
    private void UpdateFortressNextState()
    {
        var now = DateTime.Now;
        var currentHourPlus1 = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);

        foreach (var (locationId, cronExpressions) in _siegeSchedule)
        {
            if (GetSiegeLocation(locationId) is not { } location) continue;

            var nextFireDates = cronExpressions
                .Select(expr => new CronExpression(expr) { TimeZone = TimeZoneInfo.Local }.GetNextValidTimeAfter(DateTimeOffset.Now))
                .Where(d => d.HasValue)
                .Select(d => d!.Value.LocalDateTime)
                .OrderBy(d => d)
                .ToList();
            if (nextFireDates.Count == 0) continue;

            var nextSiegeDate = nextFireDates[0];
            var siegeStartHour = new DateTime(nextSiegeDate.Year, nextSiegeDate.Month, nextSiegeDate.Day, nextSiegeDate.Hour, 0, 0);
            if (location is SourceLocation)
                siegeStartHour = siegeStartHour.AddHours(1);

            var siegeCalendar = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0)
                .AddSeconds(GetRemainingSiegeTimeInSeconds(locationId));

            location.SetNextState(currentHourPlus1 == siegeStartHour || siegeCalendar > currentHourPlus1
                ? SiegeLocation.StateVulnerable
                : SiegeLocation.StateInvulnerable);
        }
    }

    /// <summary>Java updateOutpostStatusByFortress(FortressLocation) — recomputes every dependent
    /// outpost's owning race whenever one of its dependency fortresses changes hands, restarting/
    /// stopping the outpost's own siege as needed.</summary>
    public async Task UpdateOutpostStatusByFortressAsync(FortressLocation fortress, CancellationToken ct = default)
    {
        foreach (var outpost in Outposts.Values)
        {
            if (!outpost.FortressDependency.Contains(fortress.LocationId)) continue;

            SiegeRace newFortressRace;
            if (!IsRouteSpawned(outpost))
            {
                newFortressRace = fortress.Race;
                foreach (var fortressId in outpost.FortressDependency)
                {
                    if (GetFortress(fortressId) is not { } dependency) continue;
                    if (dependency.Race != newFortressRace)
                    {
                        newFortressRace = SiegeRace.BALAUR;
                        break;
                    }
                }
            }
            else
            {
                newFortressRace = outpost.LocationRace;
            }

            SiegeRace newOutpostRace = newFortressRace == SiegeRace.BALAUR
                ? SiegeRace.BALAUR
                : newFortressRace == SiegeRace.ELYOS ? SiegeRace.ASMODIANS : SiegeRace.ELYOS;

            if (outpost.Race == newOutpostRace) continue;

            await StopSiegeAsync(outpost.LocationId, ct);
            DeSpawnNpcs(outpost.LocationId);
            await SetOwnerAsync(outpost.LocationId, newOutpostRace, 0, ct);

            // note: Java's broadcastStatusAndUpdate(outpost, oldSilentraState) (SM_RIFT_ANNOUNCE for the
            // Silentera Canyon infiltration-route flag) belongs to the Tiamaranta rift subsystem — out of
            // scope for this phase, see SourceLocation/SiegeShield's P2 deferral notes.

            if (newOutpostRace == SiegeRace.BALAUR) continue;

            if (outpost.IsSiegeAllowed)
                await StartSiegeAsync(outpost.LocationId, ct);
            else
                SpawnNpcs(outpost.LocationId, newOutpostRace, SiegeModType.PEACE);
        }
    }

    // note: Java's updateTiamarantaRiftsStatus(boolean, boolean)/startPreparations/checkSiegeStart
    // (Tiamaranta Eye infiltration-route rift portals + the shared source-siege preparation sequence)
    // depend on the world-map-instance + zone/teleport framework — out of scope for this phase. Sources
    // start directly on their own schedule instead (see SiegeScheduleOptions.Sources' note).

    /// <summary>Java broadcastState(SiegeLocation) — a location's vulnerability flag flipped.
    /// No-op while <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async Task BroadcastStateAsync(SiegeLocation location, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        var packet = new SM_SIEGE_LOCATION_STATE(location);
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is null) continue;
            try { await conn.SendAsync(packet, ct); } catch { }
        }
    }

    /// <summary>Java broadcastUpdate(SiegeLocation) — full ownership/ratio resync after a capture.
    /// No-op while <see cref="SiegeOptions.Enable"/> is false (influence is still recalculated either way).</summary>
    public async Task BroadcastUpdateAsync(SiegeLocation location, CancellationToken ct = default)
    {
        _influence.Recalculate(Locations.Values);
        if (!options.Value.Enable) return;

        var influencePacket = new SM_INFLUENCE_RATIO(this);
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            try
            {
                await conn.SendAsync(new SM_SIEGE_LOCATION_INFO(location, this, player), ct);
                await conn.SendAsync(influencePacket, ct);
            }
            catch
            {
                // best-effort broadcast
            }
        }
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
