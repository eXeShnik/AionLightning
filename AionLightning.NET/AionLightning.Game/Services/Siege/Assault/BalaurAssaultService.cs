using System.Collections.Concurrent;
using AionLightning.Commons.Utils;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services.Siege.Assault;

/// <summary>
/// Java services.siegeservice.BalaurAssaultService — drives the balaur auto-assault subsystem: rolls
/// whether a freshly-started fortress/artifact siege gets an escalating balaur attack on top of the
/// regular race-vs-race siege, and tears the assault down when the siege ends.
/// Java was a static singleton (getInstance()); ported here as a plain DI singleton, per this port's
/// DI-over-statics convention — <see cref="SiegeService"/> holds the only reference and calls
/// <see cref="OnSiegeStart"/>/<see cref="OnSiegeFinish"/> from <see cref="Siege.StartSiegeAsync"/>/
/// <see cref="Siege.StopSiegeAsync"/> (both gated behind <see cref="SiegeOptions.BalaurAutoAssault"/>, see
/// SiegeService.NotifyBalaurAssaultStart/Finish). Not itself injected with <see cref="SiegeService"/> to
/// avoid a constructor cycle — callers pass whatever siege-registry access is needed per call
/// (<paramref name="registerNpc"/> below) instead.
/// </summary>
public sealed class BalaurAssaultService(
    SpawnService spawnService,
    IDataManager dataManager,
    PlayerConnectionRegistry connRegistry,
    Model.Siege.Influence influence,
    ZoneService zoneService,
    IOptions<SiegeOptions> options,
    ILogger<BalaurAssaultService> log)
{
    private const int AbyssWorldId = 400010000;

    private readonly ConcurrentDictionary<int, FortressAssault> _fortressAssaults = new();

    /// <summary>Java Siege.startSiege()'s <c>onSiegeStart(this)</c> call. Rolls assault eligibility for
    /// fortress/artifact sieges only (source/outpost sieges are always ineligible, matching Java's early
    /// return for anything that isn't a FortressSiege/ArtifactSiege) and, if eligible, schedules the
    /// assault to begin after a random 1-600s delay.</summary>
    public void OnSiegeStart(Siege siege, Action<SiegeNpc> registerNpc)
    {
        bool eligible = siege switch
        {
            FortressSiege fortressSiege => CalculateFortressAssault(fortressSiege.Location),
            ArtifactSiege artifactSiege => CalculateArtifactAssault(artifactSiege.Location),
            _ => false,
        };
        if (!eligible) return;

        NewAssault(siege, Rnd.Get(1, 600), registerNpc);
        log.LogInformation("[SIEGE] Balaur Assault scheduled on Siege ID: {Id}!", siege.SiegeLocationId);
    }

    /// <summary>Java Siege.stopSiege()'s matching <c>onSiegeFinish(this)</c> call — a no-op unless an
    /// assault was actually scheduled for this location (fortressAssaults.containsKey(locId)).</summary>
    public void OnSiegeFinish(Siege siege)
    {
        int locId = siege.SiegeLocationId;
        if (!_fortressAssaults.TryRemove(locId, out var assault)) return;

        bool bossKilled = siege.BossKilled;
        assault.FinishAssault(bossKilled);

        if (bossKilled && siege.LocationBase.Race == SiegeRace.BALAUR)
            log.LogInformation("[SIEGE] > [FORTRESS:{Id}] has been captured by Balaur Assault!", locId);
        else
            log.LogInformation("[SIEGE] > [FORTRESS:{Id}] Balaur Assault finished without capture!", locId);
    }

    /// <summary>Java startAssault(Player, int, int) — manual/GM-triggered assault start, exposed for a
    /// future GM command (none is wired in this phase). Unlike Java, the caller supplies the already-
    /// resolved <paramref name="siege"/> and <paramref name="registerNpc"/> callback directly instead of a
    /// location id + reaching into SiegeService.getInstance() itself.</summary>
    public void StartAssault(Siege siege, int delaySeconds, Action<SiegeNpc> registerNpc)
    {
        if (_fortressAssaults.ContainsKey(siege.SiegeLocationId)) return;

        NewAssault(siege, delaySeconds, registerNpc);
    }

    private bool CalculateFortressAssault(FortressLocation fortress)
    {
        bool isBalaurea = fortress.WorldId != AbyssWorldId;
        int locationId = fortress.LocationId;

        if (_fortressAssaults.ContainsKey(locationId)) return false;
        if (!CalcFortressInfluence(isBalaurea, fortress)) return false;

        // Allow only 2 balaur attacks per map, 1 per Balaurea map.
        int count = _fortressAssaults.Values.Count(fa => fa.WorldId == fortress.WorldId);
        return count < (isBalaurea ? 1 : 2);
    }

    /// <summary>Java calculateArtifactAssault(ArtifactLocation) — a direct port of Java's own unimplemented
    /// "// TODO" stub; artifact assaults never actually trigger today (upstream included).</summary>
    private static bool CalculateArtifactAssault(ArtifactLocation artifact) => false;

    private bool CalcFortressInfluence(bool isBalaurea, FortressLocation fortress)
    {
        var locationRace = fortress.Race;
        if (locationRace == SiegeRace.BALAUR || !fortress.IsVulnerable) return false;

        float rollInfluence;
        if (isBalaurea)
        {
            int ownedForts = dataManager.Sieges.Fortresses.Values.Count(fl =>
                fl.WorldId != AbyssWorldId && !_fortressAssaults.ContainsKey(fl.LocationId) && fl.Race == locationRace);
            rollInfluence = ownedForts >= 2 ? 0.25f : 0.1f;
        }
        else
        {
            rollInfluence = locationRace == SiegeRace.ASMODIANS ? influence.GlobalAsmodians : influence.GlobalElyos;
        }

        return Rnd.Get() < rollInfluence * options.Value.BalaurAssaultRate;
    }

    private void NewAssault(Siege siege, int delaySeconds, Action<SiegeNpc> registerNpc)
    {
        switch (siege)
        {
            case FortressSiege fortressSiege:
                var assault = new FortressAssault(fortressSiege, this, spawnService, dataManager, connRegistry, registerNpc, log, zoneService);
                assault.StartAssault(delaySeconds);
                _fortressAssaults[siege.SiegeLocationId] = assault;
                break;
            case ArtifactSiege artifactSiege:
                new ArtifactAssault(artifactSiege).StartAssault(delaySeconds);
                break;
        }
    }

    /// <summary>
    /// Java spawnDredgion(int) — Java assembled a multi-part "Dredgion carrier" prop (AssembledNpcTemplate/
    /// AssembledNpc/SM_NPC_ASSEMBLER) that flies a route and later drops the attacker wave.
    /// note: that assembled-NPC subsystem has no port yet (not part of this port's NPC/spawn model), so the
    /// visual carrier itself is stubbed out — only the flavor announcement survives, and the attacker wave
    /// still spawns on schedule via FortressAssault.SpawnAttackers regardless.
    /// </summary>
    public void SpawnDredgion(int spawnId)
    {
        log.LogDebug("[SIEGE] Balaur Assault: dredgion spawn id {SpawnId} (visual carrier not ported)", spawnId);

        var message = SM_SYSTEM_MESSAGE.AbyssCarrierSpawn();
        _ = Task.Run(async () =>
        {
            foreach (var conn in connRegistry.GetAll())
                if (conn.ActivePlayer is not null)
                    try { await conn.SendAsync(message); } catch { }
        });
    }
}
