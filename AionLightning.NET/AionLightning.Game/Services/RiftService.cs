using System.Collections.Concurrent;
using AionLightning.Commons.Network;
using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Rift;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java services.RiftService + services.rift.{RiftManager,RiftInformer,RiftOpenRunnable} +
/// model.rift.RiftLocation — the general abyss-invasion rift system: on a per-world cron schedule
/// (Java rift_schedule.xml / <see cref="RiftScheduleOptions"/>), opens every closed rift location in
/// that world by spawning its master (interactable) and slave (arrival) portal NPC pair plus,
/// optionally, static guard NPCs; lets a level- and entry-count-gated player teleport through via the
/// master portal; and auto-closes every currently-open rift globally after the configured duration
/// (see <see cref="ScheduleGlobalCloseAsync"/> for why this mirrors a Java quirk rather than only
/// closing the world that opened).
///
/// Data and every API here always load/stay callable; only client-visible effects (spawning,
/// broadcasting, cron arming, portal-use teleport) are gated behind <see cref="RiftOptions.Enable"/>
/// (default false) — see RiftOptions' doc comment for the rationale (unverified SM_RIFT_ANNOUNCE
/// opcode/variants).
/// </summary>
public sealed class RiftService(
    IDataManager dataManager,
    SpawnService spawnService,
    GameWorld world,
    PlayerConnectionRegistry connRegistry,
    TeleportService teleportService,
    CronService cronService,
    IOptions<RiftOptions> options,
    IOptions<RiftScheduleOptions> scheduleOptions,
    ILogger<RiftService> log)
{
    /// <summary>Java RiftOpenRunnable's magic <c>3540</c> (seconds/hour, not 3600) — leaves a ~1 minute
    /// gap before the schedule's next fire so the auto-close doesn't race the next scheduled open.</summary>
    private const int AutoCloseSecondsPerHour = 3540;

    /// <summary>Java RiftService.isRift(int) — location ids (2120-2273) are always &lt; 10000; world ids
    /// (e.g. 210020000) never are. Ported as-is even though it's a heuristic rather than a type tag.</summary>
    private const int RiftLocationIdThreshold = 10000;

    /// <summary>Java RiftInformer.getTwinId(int) subset relevant to the general rift system (the
    /// Kaisinel/Marchutan/Theobomos entries there belong to the Dimensional Vortex subsystem, out of
    /// scope here — see RiftEnum's doc comment).</summary>
    private static readonly Dictionary<int, int> TwinWorlds = new()
    {
        [210020000] = 220020000, // Eltnen <-> Morheim
        [210040000] = 220040000, // Heiron <-> Beluslan
        [210050000] = 220070000, // Inggison <-> Gelkmaros
        [220020000] = 210020000,
        [220040000] = 210040000,
        [220070000] = 210050000,
    };

    private sealed class ActiveRift
    {
        public required RiftLocation Location { get; init; }
        public required RiftEnum Definition { get; init; }
        public required Npc Master { get; init; }
        public required Npc Slave { get; init; }
        public List<Npc> Guards { get; } = [];
        public int UsedEntries;
        public DateTime OpenedAtUtc = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<int, ActiveRift> _activeByRiftId = new();
    private readonly ConcurrentDictionary<int, ActiveRift> _activeByNpcObjectId = new();
    private readonly object _closeAllLock = new();

    public IReadOnlyDictionary<int, RiftLocation> Locations => dataManager.Rifts.Locations;

    private static bool IsRiftLocationId(int id) => id < RiftLocationIdThreshold;

    /// <summary>Java RiftService.isValidId(int) — true for a known rift_location id, or for a world id
    /// hosting at least one rift_location.</summary>
    public bool IsValidId(int id) =>
        IsRiftLocationId(id) ? Locations.ContainsKey(id) : Locations.Values.Any(l => l.WorldId == id);

    // --- Open/close (Java RiftService.openRifts/closeRifts(int)/openRifts(RiftLocation,boolean)/closeRift) ---

    /// <summary>
    /// Java openRifts(int id, boolean guards) — opens a single rift_location (id &lt; 10000) or every
    /// closed rift_location in a world (id treated as a world id) that isn't already open. Returns
    /// false when the id is invalid, already open, or the rift engine is disabled.
    /// </summary>
    public bool OpenRifts(int id, bool spawnGuards)
    {
        if (!options.Value.Enable || !IsValidId(id)) return false;

        if (IsRiftLocationId(id))
        {
            if (Locations.GetValueOrDefault(id) is not { Spawned.Count: 0 } location) return false;
            OpenRift(location, spawnGuards);
            _ = BroadcastRiftsInfoAsync(location.WorldId);
            return true;
        }

        bool openedAny = false;
        foreach (var location in Locations.Values.Where(l => l.WorldId == id && l.Spawned.Count == 0))
        {
            OpenRift(location, spawnGuards);
            openedAny = true;
        }

        if (openedAny)
            _ = BroadcastRiftsInfoAsync(id);
        return openedAny;
    }

    /// <summary>Java closeRifts(int id) — closes a single rift_location or every open rift_location in
    /// a world.</summary>
    public bool CloseRifts(int id)
    {
        if (!IsValidId(id)) return false;

        if (IsRiftLocationId(id))
        {
            if (Locations.GetValueOrDefault(id) is not { Spawned.Count: > 0 } location) return false;
            CloseRift(location);
            return true;
        }

        bool closedAny = false;
        foreach (var location in Locations.Values.Where(l => l.WorldId == id && l.Spawned.Count > 0))
        {
            CloseRift(location);
            closedAny = true;
        }
        return closedAny;
    }

    /// <summary>Java RiftService.closeRifts() — closes every currently-active rift regardless of
    /// world, guarded by a lock (Java used a ReentrantLock around the same FastMap-clear sweep).</summary>
    public void CloseAllRifts()
    {
        lock (_closeAllLock)
        {
            foreach (var location in _activeByRiftId.Values.Select(a => a.Location).ToList())
                CloseRift(location);
        }
    }

    /// <summary>Java RiftService.openRifts(RiftLocation, boolean) + RiftManager.spawnRift — spawns the
    /// master/slave portal pair for this location (and, when requested, its static guard NPCs).
    /// note: Java spawned one master/slave pair per open world-map instance
    /// (<c>World.getInstance().getWorldMap(worldId).getInstanceCount()</c>); this port's rift maps run
    /// as single-instance open-world channels, so exactly one pair is spawned here.</summary>
    private void OpenRift(RiftLocation location, bool spawnGuards)
    {
        if (!options.Value.Enable) return;

        var definition = RiftEnum.GetById(location.Id);
        if (definition is null)
        {
            log.LogWarning("RiftService: no RiftEnum definition for rift location {Id}", location.Id);
            return;
        }

        var masterAnchor = dataManager.RiftSpawns.GetAnchor(definition.Master);
        var slaveAnchor = dataManager.RiftSpawns.GetAnchor(definition.Slave);
        if (masterAnchor is not { } master0 || slaveAnchor is not { } slave0)
        {
            log.LogWarning("RiftService: missing anchor spawn data for rift {Id} ({Master}/{Slave})",
                location.Id, definition.Master, definition.Slave);
            return;
        }

        var masterTemplate = dataManager.Npcs.GetTemplate(master0.NpcId);
        var slaveTemplate = dataManager.Npcs.GetTemplate(slave0.NpcId);
        if (masterTemplate is null || slaveTemplate is null)
        {
            log.LogWarning("RiftService: missing NPC template for rift {Id}", location.Id);
            return;
        }

        var master = spawnService.SpawnNpcAt(masterTemplate,
            new Position(master0.X, master0.Y, master0.Z, master0.Heading, master0.WorldId));
        var slave = spawnService.SpawnNpcAt(slaveTemplate,
            new Position(slave0.X, slave0.Y, slave0.Z, slave0.Heading, slave0.WorldId));

        var active = new ActiveRift { Location = location, Definition = definition, Master = master, Slave = slave };
        _activeByRiftId[location.Id] = active;
        _activeByNpcObjectId[master.ObjectId] = active;
        _activeByNpcObjectId[slave.ObjectId] = active;

        location.Opened = true;
        location.Spawned.Add(master);
        location.Spawned.Add(slave);

        var spawned = new List<Npc> { master, slave };

        if (spawnGuards)
        {
            foreach (var guardPoint in dataManager.RiftSpawns.GetGuards(location.Id))
            {
                var guardTemplate = dataManager.Npcs.GetTemplate(guardPoint.NpcId);
                if (guardTemplate is null) continue;

                var guard = spawnService.SpawnNpcAt(guardTemplate,
                    new Position(guardPoint.X, guardPoint.Y, guardPoint.Z, guardPoint.Heading, guardPoint.WorldId),
                    guardPoint.RespawnTime);
                active.Guards.Add(guard);
                location.Spawned.Add(guard);
                spawned.Add(guard);
            }
        }

        log.LogInformation("RiftService: opened rift {Id} ({Master} -> {Slave}), spawned {Count} NPC(s)",
            location.Id, definition.Master, definition.Slave, spawned.Count);

        _ = BroadcastSpawnAsync(spawned);
    }

    /// <summary>Java RiftService.closeRift(RiftLocation) — despawns the master/slave/guard NPCs and
    /// unregisters the rift.</summary>
    private void CloseRift(RiftLocation location)
    {
        if (!_activeByRiftId.TryRemove(location.Id, out var active))
        {
            location.Opened = false;
            location.Spawned.Clear();
            return;
        }

        _activeByNpcObjectId.TryRemove(active.Master.ObjectId, out _);
        _activeByNpcObjectId.TryRemove(active.Slave.ObjectId, out _);

        world.Remove(active.Master);
        world.Remove(active.Slave);
        foreach (var guard in active.Guards)
            world.Remove(guard);

        location.Opened = false;
        location.Spawned.Clear();

        log.LogInformation("RiftService: closed rift {Id}, despawned {Count} NPC(s)",
            location.Id, active.Guards.Count + 2);

        _ = BroadcastCloseAsync(active);
    }

    // --- Entry (Java controllers.RVController.onDialogRequest/onAccept/onRequest) ---

    /// <summary>Java RVController.onAccept(Player) — level range plus current-vs-max entry count.
    /// Returns false (and denies entry) while the rift engine is disabled.</summary>
    public bool CanEnterRift(Player player, int riftId)
    {
        if (!options.Value.Enable) return false;
        if (!_activeByRiftId.TryGetValue(riftId, out var active)) return false;
        if (player.Level < active.Definition.MinLevel || player.Level > active.Definition.MaxLevel) return false;
        return active.UsedEntries < active.Definition.Entries;
    }

    /// <summary>
    /// Java RVController.onRequest's acceptRequest branch — teleports the player to the rift's slave
    /// (arrival) coordinates and increments the used-entry count. Returns false without side effects
    /// if <see cref="CanEnterRift"/> would deny the attempt.
    /// </summary>
    public async ValueTask<bool> EnterRiftAsync(Player player, int riftId, CancellationToken ct = default)
    {
        if (!CanEnterRift(player, riftId)) return false;
        // Defensive re-fetch (rather than indexing) — the rift could have auto-closed between the
        // CanEnterRift check above and here.
        if (!_activeByRiftId.TryGetValue(riftId, out var active)) return false;

        if (dataManager.RiftSpawns.GetAnchor(active.Definition.Slave) is not { } slaveAnchor) return false;

        await teleportService.TeleportToAsync(player, slaveAnchor.WorldId, 0,
            slaveAnchor.X, slaveAnchor.Y, slaveAnchor.Z, slaveAnchor.Heading, ct: ct);

        Interlocked.Increment(ref active.UsedEntries);
        _ = BroadcastEntriesAsync(active);

        return true;
    }

    /// <summary>
    /// Entry point for the rift-portal NPC interaction (wired from CM_SHOW_DIALOG, the same place
    /// PortalService's plain-portal teleport is checked). Returns false when <paramref name="npc"/> is
    /// not a currently-open rift portal, so the caller can fall through to normal dialog handling.
    /// Only the master portal is interactable (Java's RVController.isAccepting is only ever true for
    /// the master instance) — touching the slave/arrival portal is a no-op that still reports "handled"
    /// so it doesn't fall through to a generic NPC greeting dialog either.
    /// note: Java confirmed entry with a yes/no SM_QUESTION_WINDOW (STR_ASK_PASS_BY_DIRECT_PORTAL)
    /// before teleporting. This port's NPC-interaction layer has no wired request/response round trip
    /// for AI-script-driven NPCs yet (see PortalAI2/PortalRequestAI2's own doc comments — the same
    /// simplification already applied to plain portals via PortalService), so entry happens immediately
    /// on interact, without a confirmation dialog.
    /// </summary>
    public async ValueTask<bool> TryUseRiftPortalAsync(Player player, Npc npc, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return false;
        if (!_activeByNpcObjectId.TryGetValue(npc.ObjectId, out var active)) return false;
        if (active.Master.ObjectId != npc.ObjectId) return true;

        await EnterRiftAsync(player, active.Location.Id, ct);
        return true;
    }

    // --- Scheduling (Java RiftService.initRifts/RiftOpenRunnable) ---

    /// <summary>Java initRifts() — arms one cron job per &lt;open&gt; schedule entry. No-op (cron never
    /// armed) while <see cref="RiftOptions.Enable"/> is false.</summary>
    public async Task ScheduleRiftsAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("RiftService: rift engine disabled (GameServer:Rift:Enable=false) — cron not armed.");
            return;
        }

        foreach (var (worldId, entries) in scheduleOptions.Value.Worlds)
            foreach (var entry in entries)
                await cronService.Schedule(
                    () => FireAndForget(OpenRiftsForWorldAsync(worldId, entry.SpawnGuards), $"rift open for world {worldId}"),
                    entry.Cron, longRunningTask: true);
    }

    /// <summary>Java RiftOpenRunnable.run()'s open half — opens every closed rift_location in this
    /// world, broadcasts the updated state, then arms the (global, see
    /// <see cref="ScheduleGlobalCloseAsync"/>) auto-close timer.</summary>
    private async Task OpenRiftsForWorldAsync(int worldId, bool spawnGuards)
    {
        bool openedAny = false;
        foreach (var location in Locations.Values.Where(l => l.WorldId == worldId && l.Spawned.Count == 0))
        {
            OpenRift(location, spawnGuards);
            openedAny = true;
        }

        if (!openedAny) return;

        await BroadcastRiftsInfoAsync(worldId);
        _ = ScheduleGlobalCloseAsync();
    }

    /// <summary>
    /// Java RiftOpenRunnable's ThreadPoolManager-scheduled close half — after
    /// <see cref="RiftOptions.DurationHours"/> hours (at <see cref="AutoCloseSecondsPerHour"/> seconds
    /// each), calls <see cref="CloseAllRifts"/>. note: Java's own close call here is the global
    /// <c>RiftService.closeRifts()</c>, not scoped to the world that just opened — faithfully preserved
    /// even though it means a rift opened elsewhere while this timer is still running gets closed early
    /// too (a pre-existing Java quirk, not a defect introduced by this port).
    /// </summary>
    private async Task ScheduleGlobalCloseAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.Value.DurationHours * AutoCloseSecondsPerHour));
            CloseAllRifts();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Error auto-closing rifts");
        }
    }

    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "Unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);

    // --- Broadcasts (Java services.rift.RiftInformer) ---

    private int RemainingSeconds(ActiveRift active)
    {
        int total = options.Value.DurationHours * AutoCloseSecondsPerHour;
        int elapsed = (int)(DateTime.UtcNow - active.OpenedAtUtc).TotalSeconds;
        return Math.Max(0, total - elapsed);
    }

    /// <summary>Java RiftInformer.sendRiftsInfo(int worldId) — resyncs the overview + per-master
    /// open/entries packets for this world and its twin.</summary>
    private async Task BroadcastRiftsInfoAsync(int worldId)
    {
        await SendRiftPacketsForWorldAsync(worldId);
        if (TwinWorlds.TryGetValue(worldId, out var twin))
            await SendRiftPacketsForWorldAsync(twin);
    }

    private async Task SendRiftPacketsForWorldAsync(int worldId)
    {
        var mastersHere = _activeByRiftId.Values.Where(a => a.Master.Position.WorldId == worldId).ToList();
        var slavesHere = _activeByRiftId.Values.Where(a => a.Slave.Position.WorldId == worldId).ToList();
        if (mastersHere.Count == 0 && slavesHere.Count == 0) return;

        // Java calcRiftsData: [0]=open-master count, [1]=vortex-master (always 0, no vortex here),
        // [2..4]=master count repeated three times, [5]=open-slave count, [6]=slave count repeated,
        // [7]=vortex-slave (always 0).
        var overview = new int[8];
        overview[0] = overview[2] = overview[3] = overview[4] = mastersHere.Count;
        overview[5] = overview[6] = slavesHere.Count;

        var packets = new List<AionServerPacket> { new SM_RIFT_ANNOUNCE(overview) };
        foreach (var active in mastersHere)
        {
            var pos = active.Master.Position;
            packets.Add(new SM_RIFT_ANNOUNCE(active.Master.ObjectId, active.Definition.Entries, RemainingSeconds(active),
                active.Definition.MinLevel, active.Definition.MaxLevel, pos.X, pos.Y, pos.Z));
            packets.Add(new SM_RIFT_ANNOUNCE(active.Master.ObjectId, active.UsedEntries, RemainingSeconds(active)));
        }

        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player || player.Position.WorldId != worldId) continue;
            foreach (var packet in packets)
                try { await conn.SendAsync(packet); } catch { }
        }
    }

    /// <summary>Java RiftInformer's per-master SM_RIFT_ANNOUNCE(controller, false) resync after a
    /// player passes through (used-entry count changed).</summary>
    private async Task BroadcastEntriesAsync(ActiveRift active)
    {
        var packet = new SM_RIFT_ANNOUNCE(active.Master.ObjectId, active.UsedEntries, RemainingSeconds(active));
        int worldId = active.Master.Position.WorldId;

        await SendToWorldAsync(packet, worldId);
        if (TwinWorlds.TryGetValue(worldId, out var twin))
            await SendToWorldAsync(packet, twin);
    }

    /// <summary>Java's VisibleObjectSpawner-driven SM_NPC_INFO broadcast on spawn, scoped to whoever
    /// can already see the spawn point (same pattern as SiegeService.SpawnNpcs).</summary>
    private async Task BroadcastSpawnAsync(List<Npc> npcs)
    {
        foreach (var npc in npcs)
        {
            var packet = new SM_NPC_INFO(npc);
            await SendToScopeAsync(packet, npc.Position);
        }
    }

    /// <summary>Java RVController.onDelete's RiftInformer.sendRiftDespawn(worldId, objectId) for the
    /// master/slave pair (SM_RIFT_ANNOUNCE actionId 4), plus a plain SM_DELETE for any guard NPCs
    /// (which aren't RVController-wrapped in Java either).</summary>
    private async Task BroadcastCloseAsync(ActiveRift active)
    {
        await SendToScopeAsync(new SM_RIFT_ANNOUNCE(active.Master.ObjectId), active.Master.Position);
        await SendToScopeAsync(new SM_RIFT_ANNOUNCE(active.Slave.ObjectId), active.Slave.Position);

        foreach (var guard in active.Guards)
            await SendToScopeAsync(new SM_DELETE(guard.ObjectId), guard.Position);
    }

    private async Task SendToScopeAsync(AionServerPacket packet, Position scope)
    {
        foreach (var conn in connRegistry.GetAll())
            if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                try { await conn.SendAsync(packet); } catch { }
    }

    private async Task SendToWorldAsync(AionServerPacket packet, int worldId)
    {
        foreach (var conn in connRegistry.GetAll())
            if (conn.ActivePlayer is { } p && p.Position.WorldId == worldId)
                try { await conn.SendAsync(packet); } catch { }
    }
}
