using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Siege;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services.Siege;

/// <summary>
/// Java services.siegeservice.Siege&lt;SL extends SiegeLocation&gt; — non-generic half. C# generics have
/// no existential/wildcard type parameter (Java's <c>Siege&lt;?&gt;</c>), so <see cref="SiegeService"/>'s
/// active-siege map is keyed to this base type instead, and <see cref="Siege{TLocation}"/> below adds the
/// strongly-typed <c>Location</c> accessor that <c>FortressSiege</c>/<c>SourceSiege</c>/<c>OutpostSiege</c>/
/// <c>ArtifactSiege</c> consume.
/// </summary>
/// <remarks>
/// Java wired the boss-death/damage reactions as <c>ai2</c> callback objects registered directly onto the
/// boss NPC (<c>registerSiegeBossListeners</c>, dropped AOP framework — see CLAUDE.md). This port instead
/// exposes <see cref="AddBossDamage"/> for <c>Combat.Handlers.SiegeBossDamageHandler</c> and lets
/// <c>Combat.Handlers.SiegeBossDeathHandler</c> call <see cref="StopSiegeAsync"/> directly, both driven by
/// the shared <see cref="AionLightning.Commons.Events.IEventBus"/> DeathEvent/DamageDealtEvent stream.
/// </remarks>
public abstract class Siege
{
    private int _started;
    private int _finished;

    protected readonly SiegeService Service;
    protected readonly ILogger Log;

    protected Siege(SiegeService service, ILogger log)
    {
        Service = service;
        Log = log;
    }

    /// <summary>Non-generic accessor for the siege location; see <see cref="Siege{TLocation}.Location"/>
    /// for the strongly-typed version consumed by concrete siege classes.</summary>
    public abstract SiegeLocation LocationBase { get; }

    public int SiegeLocationId => LocationBase.LocationId;

    public bool BossKilled { get; set; }

    public SiegeNpc? Boss { get; set; }

    public DateTime? StartTime { get; private set; }

    public bool Started => Volatile.Read(ref _started) != 0;

    public bool Finished => Volatile.Read(ref _finished) != 0;

    public SiegeCounter SiegeCounter { get; } = new();

    public abstract bool IsEndless { get; }

    public abstract void AddAbyssPoints(Player player, int abyssPoints);

    public abstract void AddGloryPoints(Player player, int gloryPoints);

    protected abstract Task OnSiegeStartAsync(CancellationToken ct);

    protected abstract Task OnSiegeFinishAsync(CancellationToken ct);

    /// <summary>Java startSiege() — idempotent; a double-start is logged and ignored rather than thrown,
    /// matching Java exactly.</summary>
    public async Task StartSiegeAsync(CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            Log.LogError("Attempt to start siege of SiegeLocation#{Id} for 2 times", SiegeLocationId);
            return;
        }

        StartTime = DateTime.UtcNow;
        await OnSiegeStartAsync(ct);
        // note: Java's BalaurAssaultService.onSiegeStart hook (SiegeConfig.BALAUR_AUTO_ASSAULT) is P3+ —
        // the balaur-assault subsystem (BalaurAssaultService/Assault/FortressAssault/ArtifactAssault) is
        // not ported.
    }

    /// <summary>Java stopSiege() — idempotent; a double-stop is logged and ignored rather than thrown.</summary>
    public async Task StopSiegeAsync(CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _finished, 1, 0) != 0)
        {
            Log.LogError("Attempt to stop siege of SiegeLocation#{Id} for 2 times", SiegeLocationId);
            return;
        }

        await OnSiegeFinishAsync(ct);
        // note: Java's BalaurAssaultService.onSiegeFinish hook — see StartSiegeAsync note.
    }

    /// <summary>
    /// Java addBossDamage(Creature, int) — resolves a summon's damage back onto its master (Java
    /// Creature.getMaster()) before crediting the siege counter. Called by
    /// Combat.Handlers.SiegeBossDamageHandler on every DamageDealtEvent whose target is this siege's boss.
    /// </summary>
    public void AddBossDamage(Creature attacker, int damage)
    {
        if (Finished) return;

        Creature resolved = attacker is Summon { Master: { } master } ? master : attacker;
        SiegeRace? npcRace = resolved is Npc npc ? Service.GetSiegeNpc(npc)?.SiegeRace : null;
        SiegeCounter.AddDamage(resolved, damage, npcRace);
    }

    /// <summary>
    /// Java initSiegeBoss() — locates the single BOSS-flagged siege NPC for this location.
    /// <see cref="SiegeService.GetLocalSiegeNpcs"/> is now populated by SiegeService.SpawnNpcs, but this
    /// still always throws today: Java flags the boss via NpcTemplate.getAbyssNpcType()==BOSS, and that
    /// field isn't part of this port's NPC static data yet, so <see cref="SiegeNpc.IsBoss"/> is never
    /// true. Only reachable when <see cref="Configs.Options.SiegeOptions.Enable"/> is true, since that
    /// gates both the scheduling that calls this and SpawnNpcs itself.
    /// </summary>
    protected void InitSiegeBoss()
    {
        SiegeNpc? boss = null;
        foreach (var npc in Service.GetLocalSiegeNpcs(SiegeLocationId))
        {
            if (!npc.IsBoss) continue;
            if (boss is not null)
                throw new SiegeException($"Found 2 siege bosses for outpost {SiegeLocationId}");
            boss = npc;
        }

        if (boss is null)
            throw new SiegeException($"Siege Boss not found for siege {SiegeLocationId}");

        Boss = boss;
    }

    protected void SpawnNpcs(int locationId, SiegeRace race, SiegeModType type) => Service.SpawnNpcs(locationId, race, type);

    protected void DeSpawnNpcs(int locationId) => Service.DeSpawnNpcs(locationId);

    protected Task BroadcastStateAsync(SiegeLocation location, CancellationToken ct) => Service.BroadcastStateAsync(location, ct);

    protected Task BroadcastUpdateAsync(SiegeLocation location, CancellationToken ct) => Service.BroadcastUpdateAsync(location, ct);

    protected Task UpdateOutpostStatusByFortressAsync(FortressLocation location, CancellationToken ct) =>
        Service.UpdateOutpostStatusByFortressAsync(location, ct);

    protected Task UpdateTiamarantaRiftsStatusAsync(bool isPreparation, bool isSync, CancellationToken ct) =>
        Service.UpdateTiamarantaRiftsStatusAsync(isPreparation, isSync, ct);
}

/// <summary>Java services.siegeservice.Siege&lt;SL extends SiegeLocation&gt; — strongly-typed half; see
/// the non-generic <see cref="Siege"/> base for the shared lifecycle/boss/counter machinery.</summary>
public abstract class Siege<TLocation> : Siege where TLocation : SiegeLocation
{
    public TLocation Location { get; }

    public override SiegeLocation LocationBase => Location;

    protected Siege(TLocation location, SiegeService service, ILogger log) : base(service, log)
    {
        Location = location;
    }
}
