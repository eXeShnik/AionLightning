using System.Collections.Concurrent;
using AionLightning.Commons.Network;
using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Vortex;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java services.VortexService + services.vortexservice.{DimensionalVortex,Invasion,
/// GeneratorDestroyListener} + model.vortex.{VortexLocation,VortexStateType} — the Dimensional Vortex
/// subsystem: on a per-location weekly cron schedule (Java gameserver.vortex.brusthonin.schedule/
/// gameserver.vortex.theobomos.schedule, see <see cref="VortexOptions"/>), opens an invasion by
/// despawning the location's peaceful garrison, spawning its entry portal pair (reusing
/// <see cref="RiftSpawnData"/>'s KAISINEL_AM/KAISINEL_AS and MARCHUTAN_AM/MARCHUTAN_AS anchors — the
/// same npc ids/coordinates Java's own RiftManager.spawnVortex resolves through, confirmed against
/// this repo's data/static_data/spawns/Rifts/*.xml) plus any invasion-tagged garrison NPCs
/// (<see cref="VortexSpawnData"/>), lets an accepted invader teleport through via the master portal
/// (<see cref="TryUseVortexPortalAsync"/>), and closes automatically after
/// <see cref="VortexOptions.DurationHours"/> — or immediately if the location's generator/boss NPC dies
/// first (see Combat.Handlers.VortexGeneratorDeathHandler).
///
/// Only two locations ever exist in the 4.6 data set (data/static_data/vortex/dimensional_vortex.xml):
/// id 0 (defends ELYOS, home Marchutan Priory 120080000, invades Theobomos 210060000) and id 1 (defends
/// ASMODIANS, home Kaisinel Academy 110070000, invades Brusthonin 220050000) — Java hardcoded both the
/// location ids and their portal anchor names (via <c>RiftEnum.getVortex(Race)</c>'s two isVortex()=true
/// entries, KAISINEL_AM/MARCHUTAN_AM) rather than driving them from data, and this port keeps that same
/// hardcoding (see <see cref="PortalAnchorsByLocationId"/>) since a third location was never introduced
/// upstream.
///
/// note: Java's own invader/defender tracking rode on a per-location <c>ZoneHandler</c> callback
/// (VortexLocation.onEnterZone/onLeaveZone) that fired as a player's knownlist crossed the invasion
/// zone's real polygon, feeding a full <c>PlayerAlliance</c> (auto-merging groups, replacing group
/// leadership, etc.) via PlayerGroupService/PlayerAllianceService. Neither a generic per-zone handler
/// hook (this port's <see cref="ZoneService"/> only drives quest/instance callbacks) nor a
/// PlayerGroup/PlayerAlliance service exists in this port yet, so invader/defender membership here is
/// flattened to plain id sets on <see cref="VortexLocation"/>, populated directly by portal entry
/// (<see cref="TryUseVortexPortalAsync"/>) and a best-effort defender-join prompt fired once at
/// invasion start (<see cref="PromptDefendersAsync"/>) rather than by continuous zone tracking — a
/// defender who walks into the invasion world only after it has already started is not prompted. See
/// this class's individual members for further call-by-call notes.
///
/// Data and every API here always load/stay callable; only spawning, cron arming, portal-use teleport
/// and client broadcasts are gated behind <see cref="VortexOptions.Enable"/> (default false) — see
/// VortexOptions' doc comment for the rationale.
/// </summary>
public sealed class VortexService(
    IDataManager dataManager,
    SpawnService spawnService,
    GameWorld world,
    PlayerConnectionRegistry connRegistry,
    TeleportService teleportService,
    CronService cronService,
    PlayerResponseRegistry responseRegistry,
    IOptions<VortexOptions> options,
    ILogger<VortexService> log)
{
    /// <summary>Java DimensionalVortex.initRiftGenerator's hardcoded generator npc ids (209487/209486) —
    /// never configurable upstream. Any invasion-state spawn (see <see cref="VortexSpawnData"/>) whose
    /// npc id matches one of these becomes the location's boss (<see cref="VortexLocation.Generator"/>).</summary>
    private static readonly int[] GeneratorNpcIds = [209486, 209487];

    /// <summary>Java RiftEnum.getVortex(Race)'s two isVortex()=true entries (KAISINEL_AM id 1170,
    /// MARCHUTAN_AM id 1280), reduced to the master/slave anchor name pair each vortex location always
    /// resolves through <see cref="RiftSpawnData"/> — see this class's own doc comment for why the
    /// mapping is hardcoded rather than data-driven.</summary>
    private static readonly Dictionary<int, (string Master, string Slave)> PortalAnchorsByLocationId = new()
    {
        [0] = ("MARCHUTAN_AM", "MARCHUTAN_AS"),
        [1] = ("KAISINEL_AM", "KAISINEL_AS"),
    };

    /// <summary>Java's SM_QUESTION_WINDOW code for Invasion.updateDefenders' join-the-defense prompt.</summary>
    private const int JoinDefenseQuestionCode = 904306;

    private readonly ConcurrentDictionary<int, VortexLocation> _portalNpcObjectIds = new();
    private readonly ConcurrentDictionary<int, VortexLocation> _generatorNpcObjectIds = new();

    public IReadOnlyDictionary<int, VortexLocation> Locations => dataManager.Vortices.Locations;

    public VortexLocation? GetLocation(int id) => Locations.GetValueOrDefault(id);

    /// <summary>Java VortexService.getLocationByWorld(int) — the location whose invasion world matches,
    /// used by <see cref="ValidateLoginZone"/> and quest handlers (KillInWorld/MonsterHunt) checking
    /// whether a kill happened inside an active vortex.</summary>
    public VortexLocation? GetLocationByWorld(int worldId) =>
        Locations.Values.FirstOrDefault(l => l.Start.WorldId == worldId);

    // --- Boot-time peace garrison + cron arming (Java initVortexLocations) ---

    /// <summary>Java initVortexLocations()'s unconditional per-location <c>spawn(loc, PEACE)</c> call —
    /// spawns each location's peaceful-state garrison (no portal; see this class's doc comment on why
    /// the portal only exists during an active invasion). A no-op while <see cref="VortexOptions.Enable"/>
    /// is false. Called once at startup by <see cref="VortexServiceHostedService"/>.</summary>
    public Task StartAllPeaceAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("VortexService: vortex engine disabled (GameServer:Vortex:Enable=false) — no peace garrison spawned.");
            return Task.CompletedTask;
        }

        foreach (var location in Locations.Values)
            SpawnPeaceState(location);

        return Task.CompletedTask;
    }

    /// <summary>Java initVortexLocations()'s two CronService.schedule calls (Brusthonin/Theobomos). A
    /// no-op (cron never armed) while <see cref="VortexOptions.Enable"/> is false.</summary>
    public async Task ScheduleAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("VortexService: vortex engine disabled (GameServer:Vortex:Enable=false) — cron not armed.");
            return;
        }

        await cronService.Schedule(() => FireAndForget(StartInvasionAsync(0), "vortex invasion start (Theobomos, id 0)"),
            options.Value.TheobomosCron, longRunningTask: true);
        await cronService.Schedule(() => FireAndForget(StartInvasionAsync(1), "vortex invasion start (Brusthonin, id 1)"),
            options.Value.BrusthoninCron, longRunningTask: true);
    }

    // --- Invasion start/stop (Java VortexService.startInvasion/stopInvasion + DimensionalVortex/Invasion) ---

    /// <summary>
    /// Java VortexService.startInvasion(id) + DimensionalVortex.start()/Invasion.startInvasion() —
    /// despawns the location's peace garrison, spawns the entry portal pair plus any invasion-tagged
    /// garrison NPCs, prompts online defenders already standing in the invasion world to join
    /// (<see cref="PromptDefendersAsync"/>), and arms the <see cref="VortexOptions.DurationHours"/>
    /// auto-end timer. A no-op while <see cref="VortexOptions.Enable"/> is false, the location id is
    /// unknown, or the location is already under invasion (Java's activeInvasions.containsKey guard).
    /// </summary>
    public async Task StartInvasionAsync(int id, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;
        if (GetLocation(id) is not { IsActive: false } location) return;

        location.IsActive = true;
        location.GeneratorDestroyed = false;
        location.PassedPlayers.Clear();
        location.Invaders.Clear();
        location.Defenders.Clear();

        DespawnAll(location);
        SpawnInvasionState(location);

        log.LogInformation("VortexService: invasion started for location {Id} (defends {Defends}, offence {Offence})",
            id, location.DefendsRace, location.OffenceRace);

        if (location.Master is { } master)
            await BroadcastPortalOpenAsync(location, master);

        _ = PromptDefendersAsync(location, ct);
        _ = ScheduleAutoEndAsync(id);
    }

    /// <summary>
    /// Java VortexService.stopInvasion(id) + Invasion.stopInvasion() — kicks every currently-online
    /// invader back to the location's home point, despawns the invasion-state NPCs (portal + invasion
    /// garrison), and respawns the peace-state garrison. A no-op if the location isn't currently active
    /// (Java's isInvasionInProgress guard) — safe to call both from the auto-end timer and from
    /// <see cref="Combat.Handlers.VortexGeneratorDeathHandler"/> without double-running.
    /// </summary>
    public async Task EndInvasionAsync(int id, CancellationToken ct = default)
    {
        if (GetLocation(id) is not { IsActive: true } location) return;

        location.IsActive = false;

        foreach (var invaderId in location.Invaders.ToList())
        {
            if (connRegistry.Get(invaderId) is not { ActivePlayer: { } invader } conn) continue;
            if (invader.Position.WorldId != location.Start.WorldId) continue;

            try
            {
                await teleportService.TeleportToAsync(invader, location.Home.WorldId, 0,
                    location.Home.X, location.Home.Y, location.Home.Z, location.Home.Heading, ct: ct);
                await conn.SendAsync(SM_SYSTEM_MESSAGE.VortexReturnedToEntry(), ct);
            }
            catch { }
        }

        var despawned = new List<Npc>(location.Spawned);
        int? masterId = location.Master?.ObjectId;
        int? slaveId = location.Slave?.ObjectId;

        DespawnAll(location);
        SpawnPeaceState(location);

        log.LogInformation("VortexService: invasion ended for location {Id}", id);

        await BroadcastDespawnAsync(despawned, masterId, slaveId);
    }

    /// <summary>Java DimensionalVortex's ThreadPoolManager-scheduled end half — after
    /// <see cref="VortexOptions.DurationHours"/> hours, ends the invasion unless
    /// <see cref="VortexLocation.GeneratorDestroyed"/> already did (via
    /// <see cref="Combat.Handlers.VortexGeneratorDeathHandler"/>). <see cref="EndInvasionAsync"/>'s own
    /// IsActive guard makes this safe even if both fire.</summary>
    private async Task ScheduleAutoEndAsync(int id)
    {
        try
        {
            await Task.Delay(TimeSpan.FromHours(options.Value.DurationHours));
            if (GetLocation(id) is { GeneratorDestroyed: false })
                await EndInvasionAsync(id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "VortexService: error auto-ending invasion for location {Id}", id);
        }
    }

    // --- Spawn/despawn (Java VortexService.spawn/despawn) ---

    /// <summary>Java <c>spawn(loc, PEACE)</c>'s NPC half — only VortexSpawnData's PEACE-tagged garrison
    /// NPCs; no portal (see this class's doc comment).</summary>
    private void SpawnPeaceState(VortexLocation location)
    {
        foreach (var template in dataManager.VortexSpawns.GetSpawns(location.Id, VortexSpawnState.Peace))
        {
            var npcTemplate = dataManager.Npcs.GetTemplate(template.NpcId);
            if (npcTemplate is null) continue;

            var npc = spawnService.SpawnNpcAt(npcTemplate,
                new Position(template.X, template.Y, template.Z, template.Heading, template.WorldId), template.RespawnTime);
            location.Spawned.Add(npc);
        }
    }

    /// <summary>Java RiftManager.spawnVortex(loc) + <c>spawn(loc, INVASION)</c>'s NPC half — spawns the
    /// master/slave entry portal pair (see <see cref="PortalAnchorsByLocationId"/>), then every
    /// VortexSpawnData INVASION-tagged garrison NPC, tagging the location's generator/boss (see
    /// <see cref="GeneratorNpcIds"/>) when one is found among them (Java's initRiftGenerator).</summary>
    private void SpawnInvasionState(VortexLocation location)
    {
        if (!PortalAnchorsByLocationId.TryGetValue(location.Id, out var anchors))
        {
            log.LogWarning("VortexService: no portal anchor mapping for vortex location {Id}", location.Id);
            return;
        }

        if (dataManager.RiftSpawns.GetAnchor(anchors.Master) is { } masterAnchor
            && dataManager.RiftSpawns.GetAnchor(anchors.Slave) is { } slaveAnchor
            && dataManager.Npcs.GetTemplate(masterAnchor.NpcId) is { } masterTemplate
            && dataManager.Npcs.GetTemplate(slaveAnchor.NpcId) is { } slaveTemplate)
        {
            var master = spawnService.SpawnNpcAt(masterTemplate,
                new Position(masterAnchor.X, masterAnchor.Y, masterAnchor.Z, masterAnchor.Heading, masterAnchor.WorldId));
            var slave = spawnService.SpawnNpcAt(slaveTemplate,
                new Position(slaveAnchor.X, slaveAnchor.Y, slaveAnchor.Z, slaveAnchor.Heading, slaveAnchor.WorldId));

            location.Master = master;
            location.Slave = slave;
            location.Spawned.Add(master);
            location.Spawned.Add(slave);
            _portalNpcObjectIds[master.ObjectId] = location;
            _portalNpcObjectIds[slave.ObjectId] = location;
        }
        else
        {
            log.LogWarning("VortexService: missing rift anchor/NPC template for vortex location {Id} portal ({Master}/{Slave})",
                location.Id, anchors.Master, anchors.Slave);
        }

        foreach (var template in dataManager.VortexSpawns.GetSpawns(location.Id, VortexSpawnState.Invasion))
        {
            var npcTemplate = dataManager.Npcs.GetTemplate(template.NpcId);
            if (npcTemplate is null) continue;

            var npc = spawnService.SpawnNpcAt(npcTemplate,
                new Position(template.X, template.Y, template.Z, template.Heading, template.WorldId), template.RespawnTime);
            location.Spawned.Add(npc);

            if (location.Generator is null && GeneratorNpcIds.Contains(template.NpcId))
            {
                location.Generator = npc;
                _generatorNpcObjectIds[npc.ObjectId] = location;
            }
        }

        if (location.Generator is null)
            log.LogWarning("VortexService: no generator NPC found for invasion of location {Id} — the invasion will " +
                "only end via the {Hours}h auto-timer (see VortexSpawnData's doc comment on missing invasion spawn data)",
                location.Id, options.Value.DurationHours);
    }

    /// <summary>Java despawn(loc) — removes every currently-spawned NPC (portal, generator, garrison)
    /// and clears the location's bookkeeping.</summary>
    private void DespawnAll(VortexLocation location)
    {
        foreach (var npc in location.Spawned)
        {
            world.Remove(npc);
            _portalNpcObjectIds.TryRemove(npc.ObjectId, out _);
            _generatorNpcObjectIds.TryRemove(npc.ObjectId, out _);
        }

        location.Spawned.Clear();
        location.Master = null;
        location.Slave = null;
        location.Generator = null;
    }

    // --- Generator death (called by Combat.Handlers.VortexGeneratorDeathHandler) ---

    public VortexLocation? GetLocationByGeneratorNpc(Npc npc) => _generatorNpcObjectIds.GetValueOrDefault(npc.ObjectId);

    // --- Portal entry (Java controllers.RVController.onDialogRequest/onAccept/onRequest, isVortex branch) ---

    /// <summary>Java RVController.onAccept for the isVortex branch — level range plus current-vs-max
    /// entry count. Returns false (and denies entry) while the vortex engine is disabled.</summary>
    private bool CanEnter(Player player, VortexLocation location)
    {
        if (!options.Value.Enable) return false;
        if (player.Race != location.OffenceRace) return false;
        if (player.Level < options.Value.MinLevel || player.Level > options.Value.MaxLevel) return false;
        return location.PassedPlayers.Count < options.Value.MaxEntries;
    }

    /// <summary>
    /// Entry point for the vortex portal NPC interaction (wired from CM_SHOW_DIALOG, alongside
    /// RiftService.TryUseRiftPortalAsync). Returns false when <paramref name="npc"/> is not a currently-
    /// spawned vortex portal, so the caller falls through to normal dialog handling. Only the master
    /// portal is interactable — touching the slave/arrival portal is a no-op that still reports
    /// "handled". On success, teleports the player to the location's start point, marks them passed +
    /// an invader (Java's flattened <see cref="VortexLocation.Invaders"/> — see this class's doc
    /// comment on why full PlayerAlliance formation isn't ported), and broadcasts the updated entry
    /// count. note: like RiftService's own port, there is no SM_QUESTION_WINDOW confirmation round trip
    /// here (no wired request/response for AI-script-driven NPCs yet) — entry happens immediately on
    /// interact.
    /// </summary>
    public async ValueTask<bool> TryUseVortexPortalAsync(Player player, Npc npc, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return false;
        if (!_portalNpcObjectIds.TryGetValue(npc.ObjectId, out var location)) return false;
        if (location.Master?.ObjectId != npc.ObjectId) return true;

        if (!CanEnter(player, location)) return true;

        await teleportService.TeleportToAsync(player, location.Start.WorldId, 0,
            location.Start.X, location.Start.Y, location.Start.Z, location.Start.Heading, ct: ct);

        location.PassedPlayers.Add(player.ObjectId);
        location.Invaders.Add(player.ObjectId);

        if (connRegistry.Get(player.ObjectId) is { } conn)
            try { await conn.SendAsync(SM_SYSTEM_MESSAGE.VortexBattleBegun(), ct); } catch { }

        if (location.Master is { } master)
            await BroadcastEntriesAsync(location, master);

        return true;
    }

    // --- Defender join prompt (Java DimensionalVortex/Invasion.updateAlliance/updateDefenders, simplified) ---

    /// <summary>
    /// Java Invasion.updateAlliance()'s loop over players the zone handler already tracked as "inside"
    /// — approximated here as every online <see cref="VortexLocation.DefendsRace"/> player already
    /// standing in the invasion world at the moment the invasion starts (see this class's doc comment
    /// on why continuous zone tracking isn't ported: a defender who arrives after this fires is never
    /// prompted). Each candidate gets their own fire-and-forget SM_QUESTION_WINDOW/CM_QUESTION_RESPONSE
    /// round trip (Java Invasion.updateDefenders), joining <see cref="VortexLocation.Defenders"/> on
    /// acceptance.
    /// </summary>
    private Task PromptDefendersAsync(VortexLocation location, CancellationToken ct)
    {
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            if (player.Race != location.DefendsRace) continue;
            if (player.Position.WorldId != location.Start.WorldId) continue;
            if (location.Defenders.Contains(player.ObjectId)) continue;

            _ = PromptDefenderAsync(player, conn, location, ct);
        }

        return Task.CompletedTask;
    }

    private async Task PromptDefenderAsync(Player player, GsClientConnection conn, VortexLocation location, CancellationToken ct)
    {
        var tcs = responseRegistry.RegisterPending(player.ObjectId);
        try
        {
            await conn.SendAsync(new SM_QUESTION_WINDOW(JoinDefenseQuestionCode, 0, 0), ct);
        }
        catch
        {
            responseRegistry.CancelPending(player.ObjectId);
            return;
        }

        bool accepted;
        try
        {
            accepted = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        catch
        {
            responseRegistry.CancelPending(player.ObjectId);
            return;
        }

        if (accepted && location.IsActive)
            location.Defenders.Add(player.ObjectId);
    }

    // --- Login-zone validation (Java VortexService.validateLoginZone) ---

    /// <summary>
    /// Java VortexService.validateLoginZone(player) — a player of the offending race who logs into (or
    /// re-enters) a vortex's invasion world without currently being a passed/active invader (e.g. the
    /// invasion ended while they were offline) should not be there. Returns true when the player's
    /// current position is fine as-is; false tells the caller to relocate them (Java itself snapped
    /// straight to the location's home point — this port instead defers to
    /// <see cref="PlayerEnterWorldService"/>'s existing bind-point fallback, the same convention
    /// <see cref="SiegeService.ValidateLoginZone"/> already established for its own analogous case).
    /// Always true while <see cref="VortexOptions.Enable"/> is false.
    /// </summary>
    public bool ValidateLoginZone(Player player)
    {
        if (!options.Value.Enable) return true;
        if (GetLocationByWorld(player.Position.WorldId) is not { } location) return true;
        if (player.Race != location.OffenceRace) return true;

        return location.IsActive && location.PassedPlayers.Contains(player.ObjectId);
    }

    // --- Broadcasts (Java services.rift.RiftInformer, reused for the isVortex case) ---

    private static int RemainingSeconds(VortexOptions opts) => opts.DurationHours * 3600;

    private async Task BroadcastPortalOpenAsync(VortexLocation location, Npc master)
    {
        var openPacket = new SM_RIFT_ANNOUNCE(master.ObjectId, options.Value.MaxEntries, RemainingSeconds(options.Value),
            options.Value.MinLevel, options.Value.MaxLevel, master.Position.X, master.Position.Y, master.Position.Z, isVortex: true);

        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            if (player.Position.WorldId != location.Home.WorldId && player.Position.WorldId != location.Start.WorldId) continue;
            try { await conn.SendAsync(openPacket); } catch { }
        }

        foreach (var npc in location.Spawned)
        {
            var infoPacket = new SM_NPC_INFO(npc);
            foreach (var conn in connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(npc.Position))
                    try { await conn.SendAsync(infoPacket); } catch { }
        }
    }

    private async Task BroadcastEntriesAsync(VortexLocation location, Npc master)
    {
        var packet = new SM_RIFT_ANNOUNCE(master.ObjectId, location.PassedPlayers.Count, RemainingSeconds(options.Value), isVortex: true);
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            if (player.Position.WorldId != location.Home.WorldId && player.Position.WorldId != location.Start.WorldId) continue;
            try { await conn.SendAsync(packet); } catch { }
        }
    }

    /// <summary>Java RVController.onDelete's SM_RIFT_ANNOUNCE(actionId 4) for the master/slave portal
    /// pair, plus a plain SM_DELETE for any other (generator/garrison) despawned NPC — mirrors
    /// RiftService.BroadcastCloseAsync's same master/slave-vs-guard distinction.</summary>
    private async Task BroadcastDespawnAsync(List<Npc> despawned, int? masterId, int? slaveId)
    {
        foreach (var npc in despawned)
        {
            AionServerPacket packet = npc.ObjectId == masterId || npc.ObjectId == slaveId
                ? new SM_RIFT_ANNOUNCE(npc.ObjectId)
                : new SM_DELETE(npc.ObjectId);

            foreach (var conn in connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(npc.Position))
                    try { await conn.SendAsync(packet); } catch { }
        }
    }

    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "VortexService: unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);
}
