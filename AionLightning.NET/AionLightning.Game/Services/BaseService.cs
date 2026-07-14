using System.Collections.Concurrent;
using AionLightning.Commons.Services;
using AionLightning.Commons.Utils;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Base;
using AionLightning.Game.Model.GameObjects.Base;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Model.Templates.Spawns;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Java services.BaseService + services.base.Base — the Balaurea capturable-outpost (base) subsystem.
/// Structurally this is <see cref="SiegeService"/>'s simpler sibling: one owning race per location (no
/// legion ownership, no shields, no influence ratio), a defender+boss garrison that reflects whichever
/// race currently owns the base, and a boss-death capture handled by
/// <see cref="Combat.Handlers.BaseBossDeathHandler"/> over the shared DeathEvent bus (Java wired this as
/// an ai2 <c>OnDieEventCallback</c> instead — see CLAUDE.md "Callbacks/AOP", dropped in this port).
/// note: Java's own base subsystem was left unfinished in the 4.6 source — see <see cref="BaseOptions"/>'s
/// doc comment for the two concrete breaks (a spawn-data lookup method that was never implemented, and a
/// persisted race column whose enum values didn't match what Java would ever actually write to it). This
/// port fixes both by implementing <see cref="DataHolders.BaseSpawnData"/> for real and persisting
/// ownership through the existing <see cref="SiegeRace"/> convention (ELYOS/ASMODIANS/BALAUR) instead of
/// Java's broken model.Race round-trip.
/// </summary>
public sealed class BaseService(
    IDataManager dataManager,
    IBaseDao baseDao,
    SpawnService spawnService,
    GameWorld world,
    PlayerConnectionRegistry connRegistry,
    CronService cronService,
    IOptions<BaseOptions> options,
    ILogger<BaseService> log)
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, BaseNpc>> _npcsByLocation = new();
    private readonly ConcurrentDictionary<int, BaseNpc> _npcsByObjectId = new();
    private readonly ConcurrentDictionary<int, List<BaseNpc>> _attackersByLocation = new();

    public IReadOnlyDictionary<int, BaseLocation> Locations => dataManager.Bases.Locations;

    public BaseLocation? GetBaseLocation(int id) => Locations.GetValueOrDefault(id);

    public IEnumerable<BaseLocation> GetBaseLocations(int worldId) =>
        Locations.Values.Where(l => l.WorldId == worldId);

    // --- Ownership persistence (Java BaseDAO.LoadBases/insertBase/updateBase) ---

    /// <summary>Java initBaseLocations()+BaseDAO.LoadBases — loads persisted race ownership from the DB,
    /// inserting a default (BALAUR-owned) row for any location not yet present. Called once at startup by
    /// <see cref="BaseServiceHostedService"/>, after schema migration.</summary>
    public async Task LoadPersistedOwnershipAsync(CancellationToken ct = default)
    {
        if (Locations.Count == 0)
        {
            log.LogInformation("BaseService: no base locations loaded from static data, skipping ownership load.");
            return;
        }

        var rows = await baseDao.LoadAllAsync(ct);
        var loaded = new HashSet<int>();
        foreach (var row in rows)
        {
            if (GetBaseLocation(row.Id) is not { } loc) continue;
            loc.Race = row.Race;
            loaded.Add(row.Id);
        }

        foreach (var loc in Locations.Values)
        {
            if (!loaded.Contains(loc.LocationId))
                await baseDao.UpsertAsync(loc.LocationId, loc.WorldId, loc.Race, ct);
        }

        log.LogInformation("BaseService: loaded ownership for {Count} base location(s)", Locations.Count);
    }

    // --- Defender/boss garrison lifecycle (Java services.base.Base.spawn/spawnBoss/despawn) ---

    /// <summary>Java initBases() — starts every known base. A no-op while <see cref="BaseOptions.Enable"/>
    /// is false (data/persistence still loaded either way).</summary>
    public Task StartAllAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("BaseService: base engine disabled (GameServer:Base:Enable=false) — no garrisons spawned.");
            return Task.CompletedTask;
        }

        foreach (var location in Locations.Values)
            StartBase(location.LocationId);

        return Task.CompletedTask;
    }

    /// <summary>Java services.base.Base.start()/spawn() — (re)spawns every defender/boss template whose
    /// race matches this location's current owner. Idempotent: any previously-spawned garrison for this
    /// location is cleared first.</summary>
    public void StartBase(int id)
    {
        if (!options.Value.Enable) return;
        if (GetBaseLocation(id) is not { } location) return;

        DespawnBase(id);
        SpawnDefenders(location);
    }

    private void SpawnDefenders(BaseLocation location)
    {
        var templates = dataManager.BaseSpawns.GetBaseSpawnsByBaseId(location.LocationId);
        if (templates.Count == 0) return;

        var spawned = new List<BaseNpc>();
        foreach (var template in templates)
        {
            if (template.BaseRace != location.Race) continue;
            if (template.HandlerType == BaseSpawnHandlerType.Attacker) continue; // attackers spawn only via TriggerAssaultsAsync

            if (spawnService.SpawnBaseNpc(template) is not { } baseNpc) continue;
            RegisterBaseNpc(baseNpc);
            spawned.Add(baseNpc);
        }

        BroadcastSpawn(spawned);
    }

    /// <summary>Java services.base.Base.despawn() — removes every currently-registered defender/boss NPC
    /// (and any in-progress assault wave) for this location.</summary>
    public void DespawnBase(int id)
    {
        DespawnAttackers(id);

        var localNpcs = GetLocalBaseNpcs(id).ToList();
        if (localNpcs.Count == 0) return;

        foreach (var baseNpc in localNpcs)
        {
            world.Remove(baseNpc.Npc);
            UnregisterBaseNpc(baseNpc);
        }

        BroadcastDespawn(localNpcs);
    }

    // --- Base-NPC registry (mirrors SiegeService's siege-NPC registry) ---

    public IEnumerable<BaseNpc> GetLocalBaseNpcs(int locationId) =>
        _npcsByLocation.TryGetValue(locationId, out var bucket) ? bucket.Values : [];

    public BaseNpc? GetBaseNpc(Npc npc) => _npcsByObjectId.GetValueOrDefault(npc.ObjectId);

    public void RegisterBaseNpc(BaseNpc npc)
    {
        _npcsByObjectId[npc.Npc.ObjectId] = npc;
        _npcsByLocation.GetOrAdd(npc.BaseId, _ => new ConcurrentDictionary<int, BaseNpc>())[npc.Npc.ObjectId] = npc;
    }

    public void UnregisterBaseNpc(BaseNpc npc)
    {
        _npcsByObjectId.TryRemove(npc.Npc.ObjectId, out _);
        if (_npcsByLocation.TryGetValue(npc.BaseId, out var bucket))
            bucket.TryRemove(npc.Npc.ObjectId, out _);
    }

    // --- Capture (Java services.base.BossDeathListener.onBeforeDie + BaseService.capture) ---

    /// <summary>
    /// Java BossDeathListener.onBeforeDie + BaseService.capture(id, race) — called by
    /// <see cref="Combat.Handlers.BaseBossDeathHandler"/> when a registered base boss dies. Changes the
    /// location's owning race, despawns the old garrison, spawns the new race's garrison, grants the
    /// capture buff (see <see cref="BaseOptions.CaptureBuffSkillId"/>), and persists the new ownership.
    /// A no-op if <paramref name="race"/> already owns the base.
    /// </summary>
    public async Task CaptureAsync(int id, SiegeRace race, CancellationToken ct = default)
    {
        if (GetBaseLocation(id) is not { } location) return;
        if (location.Race == race) return;

        var oldRace = location.Race;

        DespawnBase(id);
        location.Race = race;
        location.LastCaptureTime = DateTime.UtcNow;
        await baseDao.UpsertAsync(id, location.WorldId, race, ct);

        SpawnDefenders(location);
        await GrantCaptureBuffAsync(location, race, ct);

        log.LogInformation("BaseService: base {Id} captured by {Race} (was {OldRace})", id, race, oldRace);
    }

    // --- Assault schedule (Java Base.delayedAssault/chooseAttackersRace/spawnAttackers/despawnAttackers) ---

    /// <summary>Java initSieges()-equivalent cron arming — a no-op while <see cref="BaseOptions.Enable"/>
    /// is false. Called once at startup by <see cref="BaseServiceHostedService"/>, after
    /// <see cref="LoadPersistedOwnershipAsync"/> and <see cref="StartAllAsync"/>.</summary>
    public async Task ScheduleBasesAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enable)
        {
            log.LogInformation("BaseService: base engine disabled (GameServer:Base:Enable=false) — cron not armed.");
            return;
        }

        await cronService.Schedule(() => FireAndForget(TriggerAssaultsAsync(), "base assault wave"),
            options.Value.AssaultCron, longRunningTask: true);
    }

    /// <summary>
    /// Java Base.chooseAttackersRace()/spawnAttackers(Race) — for every currently-owned base not already
    /// under assault, rolls a rival race and spawns its ATTACKER-tagged garrison as a temporary raid,
    /// mirroring Java's Rnd(15,20)-minute repeating assault timer (approximated here as a fixed cron
    /// interval — see <see cref="BaseOptions.AssaultCron"/> — rather than Java's per-base randomized delay
    /// chain). Skipped for a base still mid-assault (Java's isAttacked() guard).
    /// </summary>
    private Task TriggerAssaultsAsync()
    {
        foreach (var location in Locations.Values)
        {
            if (GetLocalAttackers(location.LocationId).Count > 0) continue; // Java isAttacked()

            var attackerRace = PickRivalRace(location.Race);
            SpawnAttackers(location, attackerRace);
        }

        return Task.CompletedTask;
    }

    /// <summary>Java Base.chooseAttackersRace()'s pick among the two races that don't currently own the
    /// base.</summary>
    private static SiegeRace PickRivalRace(SiegeRace ownerRace)
    {
        SiegeRace[] allRaces = [SiegeRace.ELYOS, SiegeRace.ASMODIANS, SiegeRace.BALAUR];
        var candidates = new List<SiegeRace>(2);
        foreach (var race in allRaces)
            if (race != ownerRace) candidates.Add(race);

        return candidates[Rnd.Get(candidates.Count)];
    }

    private void SpawnAttackers(BaseLocation location, SiegeRace attackerRace)
    {
        var templates = dataManager.BaseSpawns.GetBaseSpawnsByBaseId(location.LocationId);
        var attackers = new List<BaseNpc>();
        foreach (var template in templates)
        {
            if (template.BaseRace != attackerRace || template.HandlerType != BaseSpawnHandlerType.Attacker) continue;
            if (spawnService.SpawnBaseNpc(template) is not { } baseNpc) continue;
            RegisterBaseNpc(baseNpc);
            attackers.Add(baseNpc);
        }

        if (attackers.Count == 0) return;

        _attackersByLocation[location.LocationId] = attackers;
        BroadcastSpawn(attackers);

        _ = ScheduleAttackerDespawnAsync(location.LocationId);
    }

    /// <summary>Java Base.stopAssault's 5-minute delayed despawnAttackers() call.</summary>
    private async Task ScheduleAttackerDespawnAsync(int locationId)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.Value.AssaultDurationSeconds));
            DespawnAttackers(locationId);
        }
        catch (Exception e)
        {
            log.LogError(e, "BaseService: error despawning assault wave for base {Id}", locationId);
        }
    }

    private List<BaseNpc> GetLocalAttackers(int locationId) =>
        _attackersByLocation.TryGetValue(locationId, out var list) ? list : [];

    /// <summary>Java Base.despawnAttackers().</summary>
    private void DespawnAttackers(int locationId)
    {
        if (!_attackersByLocation.TryRemove(locationId, out var attackers) || attackers.Count == 0) return;

        foreach (var baseNpc in attackers)
        {
            world.Remove(baseNpc.Npc);
            UnregisterBaseNpc(baseNpc);
        }

        BroadcastDespawn(attackers);
    }

    // --- Broadcasts (mirrors SiegeService.SpawnNpcs/DeSpawnNpcs) ---

    private void BroadcastSpawn(List<BaseNpc> spawned)
    {
        if (spawned.Count == 0) return;

        _ = Task.Run(async () =>
        {
            foreach (var baseNpc in spawned)
            {
                var infoPacket = new SM_NPC_INFO(baseNpc.Npc);
                var scope = baseNpc.Npc.Position;
                foreach (var conn in connRegistry.GetAll())
                    if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                        try { await conn.SendAsync(infoPacket); } catch { }
            }
        });
    }

    private void BroadcastDespawn(List<BaseNpc> removed)
    {
        if (removed.Count == 0) return;

        _ = Task.Run(async () =>
        {
            foreach (var baseNpc in removed)
            {
                var deletePacket = new SM_DELETE(baseNpc.Npc.ObjectId);
                var scope = baseNpc.Npc.Position;
                foreach (var conn in connRegistry.GetAll())
                    if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                        try { await conn.SendAsync(deletePacket); } catch { }
            }
        });
    }

    // --- Capture buff (Java's own base-capture flow never shipped one — see BaseOptions.CaptureBuffSkillId) ---

    private async Task GrantCaptureBuffAsync(BaseLocation location, SiegeRace race, CancellationToken ct)
    {
        if (options.Value.CaptureBuffSkillId <= 0) return;

        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player) continue;
            if (player.Position.WorldId != location.WorldId) continue;
            if (SiegeRaceExtensions.FromPlayerRace(player.Race) != race) continue;

            await ApplyCaptureBuffAsync(player, options.Value.CaptureBuffSkillId, ct);
        }
    }

    /// <summary>Minimal bookkeeping-only buff grant (no stat-delta calculation) — see
    /// <see cref="BaseOptions.CaptureBuffSkillId"/>'s doc comment on why there's no canonical skill/effect
    /// data to size a real stat buff from.</summary>
    private async Task ApplyCaptureBuffAsync(Player player, int skillId, CancellationToken ct)
    {
        if (player.GetActiveEffects().Any(e => e.SkillId == skillId)) return;

        player.AddEffect(new AbnormalState
        {
            SkillId = skillId,
            EffectorId = player.ObjectId,
            Expiry = DateTime.MaxValue,
        });

        var effectPacket = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        var scope = player.Position;
        foreach (var conn in connRegistry.GetAll())
            if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                try { await conn.SendAsync(effectPacket, ct); } catch { }
    }

    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "BaseService: unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);
}
