using System.Collections.Concurrent;
using AionLightning.Commons.Services;
using AionLightning.Game.Combat.Effects;
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
    TeleportService teleportService,
    IOptions<SiegeOptions> options,
    IOptions<SiegeScheduleOptions> scheduleOptions,
    ILogger<SiegeService> log)
{
    // Java SiegeService.SIEGE_LOCATION_STATUS_BROADCAST_SCHEDULE — hourly fortress-status resync.
    private const string StatusBroadcastCron = "0 0 * ? * *";

    // Java hardcoded world ids used throughout the Tiamaranta rift / fortress-buff / login-zone logic.
    private const int TiamarantaEyeWorldId = 600040000;
    private const int TiamarantaWorldId = 600030000;
    private const int SillusWorldId = 600050000;
    private const int SilonaWorldId = 600060000;

    // Java checkSiegeStart(locationId): only source 4011's own cron fire actually starts anything — it
    // drives startPreparations(), which starts all four source sieges together ~300s later.
    private const int TiamarantaPrepSourceId = 4011;

    private readonly Influence _influence = new();
    private readonly ConcurrentDictionary<int, SiegeInstance> _activeSieges = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, SiegeNpc>> _siegeNpcsByLocation = new();
    private readonly ConcurrentDictionary<int, SiegeNpc> _siegeNpcsByObjectId = new();
    private readonly Dictionary<int, List<string>> _siegeSchedule = new();

    // Java SiegeService's cl/cr/tl/tr — Tiamaranta's Eye infiltration route status: cl = Western
    // entrance, cr = Eastern entrance, tl = Elyos abyss gate, tr = Asmodian abyss gate.
    private volatile bool _cl, _cr, _tl, _tr;
    private readonly ConcurrentDictionary<int, Npc> _tiamarantaPortals = new();

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
            await ScheduleLocationAsync(locationId, cronExpressions, () => StartSiegeAsync(locationId));

        // Java checkSiegeStart(locationId): a source's own cron fire is a no-op unless it's location
        // 4011 — that one drives startPreparations(), which resets/starts all four sources together.
        // The other three still get their cron tracked in _siegeSchedule so UpdateFortressNextState can
        // compute their own next-vulnerable-state window, matching Java (whose SiegeStartRunnable is
        // likewise registered for every source even though only 4011's firing does anything).
        foreach (var (locationId, cronExpressions) in scheduleOptions.Value.Sources)
        {
            Func<Task>? onFire = locationId == TiamarantaPrepSourceId ? () => StartPreparationsAsync() : null;
            await ScheduleLocationAsync(locationId, cronExpressions, onFire);
        }

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

    private async Task ScheduleLocationAsync(int locationId, List<string> cronExpressions, Func<Task>? onFire)
    {
        _siegeSchedule[locationId] = cronExpressions;
        if (onFire is null) return;

        foreach (var cron in cronExpressions)
            await cronService.Schedule(() => FireAndForget(onFire(), $"siege start for location {locationId}"), cron, longRunningTask: true);
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

            bool oldSilenteraState = outpost.IsSilenteraAllowed;

            await StopSiegeAsync(outpost.LocationId, ct);
            DeSpawnNpcs(outpost.LocationId);
            await SetOwnerAsync(outpost.LocationId, newOutpostRace, 0, ct);

            await BroadcastStatusAndUpdateAsync(outpost, oldSilenteraState, ct);

            if (newOutpostRace == SiegeRace.BALAUR) continue;

            if (outpost.IsSiegeAllowed)
                await StartSiegeAsync(outpost.LocationId, ct);
            else
                SpawnNpcs(outpost.LocationId, newOutpostRace, SiegeModType.PEACE);
        }
    }

    /// <summary>Java broadcastState(SiegeLocation) — a location's vulnerability flag flipped.
    /// Also refreshes every online player's fortress shrine buff (Java's shared broadcast() helper calls
    /// fortressBuffRemove/fortressBuffApply before every siege-state packet). No-op while
    /// <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async Task BroadcastStateAsync(SiegeLocation location, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        var packet = new SM_SIEGE_LOCATION_STATE(location);
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            FortressBuffRemove(player);
            FortressBuffApply(player);
            try { await conn.SendAsync(packet, ct); } catch { }
        }
    }

    /// <summary>Java broadcastUpdate(SiegeLocation) — full ownership/ratio resync after a capture. Also
    /// refreshes every online player's fortress shrine buff (see <see cref="BroadcastStateAsync"/>).
    /// No-op while <see cref="SiegeOptions.Enable"/> is false (influence is still recalculated either way).</summary>
    public async Task BroadcastUpdateAsync(SiegeLocation location, CancellationToken ct = default)
    {
        _influence.Recalculate(Locations.Values);
        if (!options.Value.Enable) return;

        var influencePacket = new SM_INFLUENCE_RATIO(this);
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            FortressBuffRemove(player);
            FortressBuffApply(player);
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

    /// <summary>Java onPlayerLogin(Player) — siege ownership + influence ratio, plus both
    /// SM_RIFT_ANNOUNCE variants (Silentera Canyon outpost state, Tiamaranta's Eye rift state). No-op
    /// while <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async ValueTask OnPlayerLoginAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        await conn.SendAsync(new SM_INFLUENCE_RATIO(this), ct);
        await conn.SendAsync(new SM_SIEGE_LOCATION_INFO(this, player), ct);

        bool gelkmaros = GetOutpost(3111)?.IsSilenteraAllowed ?? false;
        bool inggison = GetOutpost(2111)?.IsSilenteraAllowed ?? false;
        await conn.SendAsync(new SM_RIFT_ANNOUNCE(gelkmaros, inggison), ct);
        await conn.SendAsync(new SM_RIFT_ANNOUNCE(_cl, _cr, _tl, _tr), ct);
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

    /// <summary>
    /// Java validateLoginZone(Player) — returns false when the player must be relocated to their bind
    /// point (inside a hostile-owned/besieged fortress zone, or the Tiamaranta's Eye rift they'd need is
    /// closed), true otherwise (including the "already relocated to a source's entry point" case).
    /// Always returns true (no-op) while <see cref="SiegeOptions.Enable"/> is false.
    /// </summary>
    public bool ValidateLoginZone(Player player)
    {
        if (!options.Value.Enable) return true;

        if (player.Position.WorldId == TiamarantaEyeWorldId)
        {
            // Elyos need the western rift (cl) open; Asmodians need the eastern rift (cr) open, or
            // tolerate its absence while source 4011 is still in its preparation window.
            return player.Race == Race.ELYOS
                ? _cl
                : _cr || (GetSource(TiamarantaPrepSourceId)?.IsPreparation ?? false);
        }

        // note: Java's isInActiveSiegeZone(player) tests the live SIEGE ZoneType instance the player's
        // knownlist places them in (isVulnerable() && isInsideLocation(player)) — that zone/knownlist
        // framework isn't ported yet (see SiegeLocation's P2 doc comment). Approximated here as
        // "same world id as the vulnerable location" — coarser than Java's actual zone polygon, so a
        // player merely sharing the open-world map with a besieged fortress (not literally inside its
        // siege perimeter) may be relocated slightly more aggressively than Java would.
        foreach (var fortress in Fortresses.Values)
        {
            if (fortress.IsVulnerable && fortress.WorldId == player.Position.WorldId && fortress.IsEnemy(player.Race))
                return false;
        }

        foreach (var source in Sources.Values)
        {
            if (source.IsVulnerable && source.WorldId == player.Position.WorldId && source.GetEntryPosition() is { } entry)
            {
                player.Position = entry;
                return true;
            }
        }

        return true;
    }

    // --- Tiamaranta rift subsystem (Java updateTiamarantaRiftsStatus/spawnTiamarantaPortels/
    // deSpawnTiamarantaPortals/startPreparations/broadcastStatusAndUpdate) ---

    /// <summary>
    /// Java updateTiamarantaRiftsStatus(boolean isPreparation, boolean isSync) — recomputes whether the
    /// Tiamaranta's Eye rift portals should be open, based on how many of the four Tiamaranta sources
    /// each race currently controls. No-op while <see cref="SiegeOptions.Enable"/> is false.
    /// </summary>
    public async Task UpdateTiamarantaRiftsStatusAsync(bool isPreparation, bool isSync, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        if (isPreparation)
        {
            await BroadcastStatusAndUpdateAsync(aSources: 0, eSources: 0, isPreparation, isSync, ct);
            return;
        }

        int sourceState = 0, aSources = 0, eSources = 0;
        foreach (var source in Sources.Values)
        {
            sourceState += source.IsVulnerable ? 0 : 1;
            if (source.Race == SiegeRace.ASMODIANS) aSources++;
            else if (source.Race == SiegeRace.ELYOS) eSources++;
        }

        // sourceState == 4 -> all four source sieges are over or not started
        if (sourceState == 4)
            await BroadcastStatusAndUpdateAsync(aSources, eSources, isPreparation, isSync, ct);
    }

    /// <summary>Java broadcastStatusAndUpdate(OutpostLocation, boolean oldSilentraState) — announces a
    /// Silentera Canyon infiltration-route (outpost) open/close, then resyncs SM_RIFT_ANNOUNCE for both
    /// Balaurea outposts. No-op while <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async Task BroadcastStatusAndUpdateAsync(OutpostLocation outpost, bool oldSilenteraState, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        SM_SYSTEM_MESSAGE? info = null;
        if (oldSilenteraState != outpost.IsSilenteraAllowed)
        {
            info = outpost.IsSilenteraAllowed
                ? (outpost.LocationId == 2111 ? SM_SYSTEM_MESSAGE.FieldAbyssLightUnderpassSpawn() : SM_SYSTEM_MESSAGE.FieldAbyssDarkUnderpassSpawn())
                : (outpost.LocationId == 2111 ? SM_SYSTEM_MESSAGE.FieldAbyssLightUnderpassDespawn() : SM_SYSTEM_MESSAGE.FieldAbyssDarkUnderpassDespawn());
        }

        bool gelkmaros = GetOutpost(3111)?.IsSilenteraAllowed ?? false;
        bool inggison = GetOutpost(2111)?.IsSilenteraAllowed ?? false;
        await BroadcastRiftAsync(new SM_RIFT_ANNOUNCE(gelkmaros, inggison), info, ct);
    }

    /// <summary>Java broadcastStatusAndUpdate(int aSources, int eSources, boolean isPreparation, boolean
    /// isSync) — recomputes the Tiamaranta's Eye rift flags from each race's controlled-source count,
    /// (re)spawns the rift portals, and schedules the delayed "abyss gate" opening 1h30m after the
    /// entrance rifts appear. No-op while <see cref="SiegeOptions.Enable"/> is false.</summary>
    public async Task BroadcastStatusAndUpdateAsync(int aSources, int eSources, bool isPreparation, bool isSync, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        DeSpawnTiamarantaPortals();
        _cl = eSources > 1;
        _cr = aSources > 1;

        if (isSync)
        {
            _tl = _cl;
            _tr = _cr;
            SpawnTiamarantaPortals(_cl, _cr, _tl, _tr);
        }
        else if (!isPreparation && (_cl || _cr))
        {
            _ = ScheduleAbyssGateOpenAsync(ct);
            SpawnTiamarantaPortals(_cl, _cr, tl: false, tr: false);
        }

        await BroadcastRiftAsync(new SM_RIFT_ANNOUNCE(_cl, _cr, _tl, _tr), null, ct);
    }

    /// <summary>Java's 5400000ms (1h30m) delayed inner Runnable inside broadcastStatusAndUpdate(int,int,
    /// boolean,boolean) — opens the Elyos/Asmodian Eye Abyss Gate rifts if they haven't already been
    /// opened by a sync in the meantime.</summary>
    private async Task ScheduleAbyssGateOpenAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5_400_000), ct);
            if (_tl && _tr) return;

            _tl = _cl;
            _tr = _cr;
            SpawnTiamarantaPortals(cl: false, cr: false, _tl, _tr);
            await BroadcastRiftAsync(new SM_RIFT_ANNOUNCE(_cl, _cr, _tl, _tr), null, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Error opening Tiamaranta's Eye abyss gate rift");
        }
    }

    /// <summary>Java spawnTiamarantaPortels(boolean,boolean,boolean,boolean) — hardcoded Tiamaranta's
    /// Eye rift portal coordinates, ported as-is (Java itself marks these "TODO: move to datapack").</summary>
    private void SpawnTiamarantaPortals(bool cl, bool cr, bool tl, bool tr)
    {
        if (cl) SpawnTiamarantaPortal(701286, 1524.450f, 1250.425f, 247.048f, 60);
        if (cr) SpawnTiamarantaPortal(701287, 1526.465f, 1784.999f, 250.436f, 60);
        if (tl) SpawnTiamarantaPortal(701288, 116.665f, 1543.754f, 295.997f, 0);
        if (tr) SpawnTiamarantaPortal(701289, 117.260f, 1929.155f, 295.691f, 0);
    }

    // note: Java also stamped a client-side decoration "staticId" (1594/2282/681/680) onto each portal's
    // spawn template — a purely visual reference this port's Npc model has no field for; the portal NPC
    // used for the actual rift teleport interaction is unaffected.
    private void SpawnTiamarantaPortal(int npcId, float x, float y, float z, int heading)
    {
        if (dataManager.Npcs.GetTemplate(npcId) is not { } template) return;

        var npc = spawnService.SpawnNpcAt(template, new Position(x, y, z, heading, TiamarantaWorldId));
        _tiamarantaPortals[npcId] = npc;

        var infoPacket = new SM_NPC_INFO(npc);
        var scope = npc.Position;
        _ = Task.Run(async () =>
        {
            foreach (var conn in connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                    try { await conn.SendAsync(infoPacket); } catch { }
        });
    }

    /// <summary>Java deSpawnTiamarantaPortals() — removes any currently-spawned rift portals and resets
    /// the route flags. Java's parallel despawn of the Tiamaranta's Eye world boss (Sunayaka) is not
    /// ported — that world-boss spawn schedule is out of scope for this phase (see SiegeOptions's doc
    /// comment on unported world-boss schedules).</summary>
    private void DeSpawnTiamarantaPortals()
    {
        _cl = _cr = _tl = _tr = false;
        if (_tiamarantaPortals.IsEmpty) return;

        var portals = _tiamarantaPortals.Values.ToList();
        _tiamarantaPortals.Clear();

        foreach (var portal in portals)
            world.Remove(portal);

        _ = Task.Run(async () =>
        {
            foreach (var portal in portals)
            {
                var deletePacket = new SM_DELETE(portal.ObjectId);
                var scope = portal.Position;
                foreach (var conn in connRegistry.GetAll())
                    if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                        try { await conn.SendAsync(deletePacket); } catch { }
            }
        });
    }

    /// <summary>Java broadcast(SM_RIFT_ANNOUNCE, SM_SYSTEM_MESSAGE) — sends the rift packet to everyone,
    /// and the accompanying flavor message only to players currently in a Balaurea-siege map.</summary>
    private async Task BroadcastRiftAsync(SM_RIFT_ANNOUNCE rift, SM_SYSTEM_MESSAGE? info, CancellationToken ct)
    {
        // note: Java gated `info` on player.getWorldType() == WorldType.BALAUREA, a world-type enum this
        // port hasn't added (see SiegeOptions's doc comment on unported subsystems). Approximated as
        // "currently in one of this server's siege-location worlds", since Balaurea is exactly the set
        // of maps that host fortresses/outposts/sources.
        var balaureaWorldIds = Locations.Values.Select(l => l.WorldId).ToHashSet();

        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            try
            {
                await conn.SendAsync(rift, ct);
                if (info is not null && balaureaWorldIds.Contains(player.Position.WorldId))
                    await conn.SendAsync(info, ct);
            }
            catch
            {
                // best-effort broadcast
            }
        }
    }

    // --- Source-siege preparation sequence (Java startPreparations) ---

    /// <summary>
    /// Java startPreparations() — the shared pre-siege sequence for all four Tiamaranta sources: reset
    /// every non-Balaur source's ownership to Balaur immediately, then 300s later evict anyone left in
    /// Tiamaranta's Eye and start all four source sieges together, then 310s later clear the shield/state
    /// display. Fired by source 4011's own cron entry (see <see cref="ScheduleSieges"/>). No-op while
    /// <see cref="SiegeOptions.Enable"/> is false.
    /// </summary>
    public async Task StartPreparationsAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        log.LogDebug("SiegeService: starting preparations of all source locations");

        foreach (var source in Sources.Values)
            source.IsPreparation = true;

        _ = ScheduleSourceSiegeStartAsync(ct);
        _ = ScheduleSourceClearAsync(ct);

        foreach (var source in Sources.Values.Where(s => s.Race != SiegeRace.BALAUR))
        {
            DeSpawnNpcs(source.LocationId);
            await SetOwnerAsync(source.LocationId, SiegeRace.BALAUR, 0, ct);
            SpawnNpcs(source.LocationId, SiegeRace.BALAUR, SiegeModType.PEACE);

            // note: Java's per-player SM_SYSTEM_MESSAGE(1301037/1301039) "ownership reset to Balaur" chat
            // announcement + immediate per-source SM_SIEGE_LOCATION_INFO repaint are skipped here —
            // SetOwnerAsync/SpawnNpcs above already keep the real ownership/NPC state correct, and the
            // next status broadcast resyncs every client's view regardless; only the flavor text is omitted.
        }

        await UpdateTiamarantaRiftsStatusAsync(isPreparation: true, isSync: false, ct);
    }

    /// <summary>Java's 300s-delayed inner Runnable inside startPreparations() — evicts anyone still in
    /// Tiamaranta's Eye to their bind point, then starts all four source sieges.</summary>
    private async Task ScheduleSourceSiegeStartAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(300), ct);

            foreach (var player in world.GetAll().Where(p => p.Position.WorldId == TiamarantaEyeWorldId).ToList())
            {
                var bind = ResolveBindPosition(player);
                await teleportService.TeleportToAsync(player, bind.WorldId, 0, bind.X, bind.Y, bind.Z, (byte)bind.Heading, ct: ct);
            }

            foreach (var source in Sources.Values)
                await StartSiegeAsync(source.LocationId, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Error starting source sieges after preparation delay");
        }
    }

    /// <summary>Java's 310s-delayed inner Runnable inside startPreparations() — broadcasts the shield
    /// effect + siege-location-state(2) display to everyone currently in the Tiamaranta source map.</summary>
    private async Task ScheduleSourceClearAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(310), ct);

            foreach (var conn in connRegistry.GetAll())
            {
                if (conn.ActivePlayer is not { } player || player.Position.WorldId != TiamarantaWorldId) continue;
                foreach (var source in Sources.Values)
                {
                    try
                    {
                        await conn.SendAsync(new SM_SHIELD_EFFECT(source.LocationId, this), ct);
                        await conn.SendAsync(new SM_SIEGE_LOCATION_STATE(source.LocationId, 2), ct);
                    }
                    catch
                    {
                        // best-effort broadcast
                    }
                }
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Error clearing source preparation display");
        }
    }

    /// <summary>Java Player bind-or-spawn fallback (same pattern used by PlayerEnterWorldService/
    /// CM_REVIVE) — used to relocate players evicted from Tiamaranta's Eye during preparation.</summary>
    private Position ResolveBindPosition(Player player)
    {
        if (player.BindPosition.HasValue) return player.BindPosition.Value;

        var spawn = dataManager.PlayerInitial.GetSpawnLocation(player.Race);
        return new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
    }

    // --- Fortress shrine buffs (Java fortressBuffApply/fortressBuffRemove) ---

    /// <summary>
    /// Java fortressBuffApply(Player) — grants the "Commendation" (own race currently controls the
    /// shrine) or "Encouragement" (a different, non-Balaur race controls it) abyss buff to a player
    /// standing in the Sillus/Silona/Pradeth fortress zones, based on each shrine's current owning race.
    /// No-op while <see cref="SiegeOptions.Enable"/> is false.
    /// </summary>
    public void FortressBuffApply(Player player)
    {
        if (!options.Value.Enable) return;

        switch (player.Position.WorldId)
        {
            case SillusWorldId:
                ApplyShrineBuff(player, GetSiegeLocation(5011), commendationSkillId: 12135, encouragementSkillId: 12136);
                break;
            case SilonaWorldId:
                ApplyShrineBuff(player, GetSiegeLocation(6011), commendationSkillId: 12137, encouragementSkillId: 12138);
                ApplyShrineBuff(player, GetSiegeLocation(6021), commendationSkillId: 12139, encouragementSkillId: 12140);
                break;
        }
    }

    private void ApplyShrineBuff(Player player, SiegeLocation? shrine, int commendationSkillId, int encouragementSkillId)
    {
        if (shrine is null) return;

        var playerSiegeRace = SiegeRaceExtensions.FromPlayerRace(player.Race);
        int? skillId = shrine.Race == playerSiegeRace ? commendationSkillId
                     : shrine.Race != SiegeRace.BALAUR ? encouragementSkillId
                     : null;
        if (skillId is not { } id) return;

        ApplyBuffSkill(player, id);
    }

    /// <summary>
    /// Java SkillEngine.getInstance().applyEffectDirectly(skillId, player, player, 0) — this port has no
    /// standalone "apply a skill's buff outside of a live cast" service (the full pipeline lives inline
    /// in CM_CASTSPELL). Reuses the same pure stat-delta calculator CM_CASTSPELL relies on
    /// (<see cref="StatEffectCalculator"/>) for the common combat-stat subset; shield/mp-shield/one-time-
    /// crit-or-attack/hide/transform/periodic-mp special-case fields (irrelevant for these plain shrine
    /// buffs) are intentionally left at their zero default rather than duplicating that whole pipeline.
    /// note: falls back to bookkeeping-only (an effect entry with no stat deltas) if the skill id has no
    /// loaded template.
    /// </summary>
    private void ApplyBuffSkill(Player player, int skillId)
    {
        if (player.GetActiveEffects().Any(e => e.SkillId == skillId)) return;

        var template = dataManager.Skills.GetTemplate(skillId);
        if (template is null)
        {
            player.AddEffect(new AbnormalState { SkillId = skillId, EffectorId = player.ObjectId, Expiry = DateTime.MaxValue });
            return;
        }

        var stat = StatEffectCalculator.Compute(template, player, level: 0);
        int durationMs = template.Duration > 0 ? template.Duration : template.Effects?.EffectDuration ?? 0;

        player.AddEffect(new AbnormalState
        {
            SkillId = skillId,
            EffectorId = player.ObjectId,
            Expiry = durationMs > 0 ? DateTime.UtcNow.AddMilliseconds(durationMs) : DateTime.MaxValue,
            SpeedStatUpPct = stat.SpeedStatUpPct,
            PreBuffMovSpeed = player.MovementSpeed,
            MaxHpDelta = stat.MaxHpDelta,
            MaxMpDelta = stat.MaxMpDelta,
            MagicBoostDeltaVal = stat.MagicBoostDeltaVal,
            HealBoostDeltaVal = stat.HealBoostDeltaVal,
            PhysAccDeltaVal = stat.PhysAccDeltaVal,
            MagicAccDeltaVal = stat.MagicAccDeltaVal,
            ParryDeltaVal = stat.ParryDeltaVal,
            BlockDeltaVal = stat.BlockDeltaVal,
            PhysCritDeltaVal = stat.PhysCritDeltaVal,
            MagicCritDeltaVal = stat.MagicCritDeltaVal,
            PhysCritResistDeltaVal = stat.PhysCritResistDeltaVal,
            MagicCritResistDeltaVal = stat.MagicCritResistDeltaVal,
            StrikeFortitudeDeltaVal = stat.StrikeFortitudeDeltaVal,
            SpellFortitudeDeltaVal = stat.SpellFortitudeDeltaVal,
            CastTimeDeltaVal = stat.CastTimeDeltaVal,
            ConcentrationDeltaVal = stat.ConcentrationDeltaVal,
            MagicSuppressionDeltaVal = stat.MagicSuppressionDeltaVal,
            PdefStatUpDeltaVal = stat.PdefStatUpDeltaVal,
            MagicDefDeltaVal = stat.MagicDefDeltaVal,
            PatkStatUpDeltaVal = stat.PatkStatUpDeltaVal,
            MagicAtkStatUpDeltaVal = stat.MagicAtkStatUpDeltaVal,
            EvasionStatUpDeltaVal = stat.EvasionStatUpDeltaVal,
            MResistStatUpDeltaVal = stat.MResistStatUpDeltaVal,
            AtkSpeedStatUpDeltaVal = stat.AtkSpeedStatUpDeltaVal,
        });
    }

    /// <summary>
    /// Java fortressBuffRemove(Player) — clears whichever shrine buff family the player currently holds.
    /// Java's if/elseif chain checks 12135..12140 individually but every branch from 12137 onward removes
    /// the identical four-id set, so it's collapsed here into one combined check. No-op while
    /// <see cref="SiegeOptions.Enable"/> is false.
    /// </summary>
    public void FortressBuffRemove(Player player)
    {
        if (!options.Value.Enable) return;

        var activeSkillIds = player.GetActiveEffects().Select(e => e.SkillId).ToHashSet();

        if (activeSkillIds.Contains(12135)) player.RemoveEffectBySkillId(12135);
        else if (activeSkillIds.Contains(12136)) player.RemoveEffectBySkillId(12136);
        else if (activeSkillIds.Overlaps([12137, 12138, 12139, 12140]))
        {
            player.RemoveEffectBySkillId(12137);
            player.RemoveEffectBySkillId(12138);
            player.RemoveEffectBySkillId(12139);
            player.RemoveEffectBySkillId(12140);
        }
    }
}
