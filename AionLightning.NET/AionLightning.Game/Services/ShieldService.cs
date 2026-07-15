using System.Collections.Concurrent;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of the fortress shield-generator half of Java's siege shield machinery
/// (ai.siege.ShieldNpcAI2 + model.siege.SiegeLocation.isUnderShield/setUnderShield): while a fortress's
/// shield-generator NPC(s) (Java <c>@AIName("siege_shieldnpc")</c>, e.g. npc_templates.xml's "aetheric
/// field engineer" set) are alive, the fortress is under-shield; once every generator has died the shield
/// drops. Java toggled this per single NPC on spawn(true)/despawn(false) — this port generalizes to a
/// live-count per fortress so it behaves identically for the common one-generator case but also handles a
/// fortress with several generators (shield stays up until the last one falls), which is what
/// <see cref="Combat.Handlers.ShieldGeneratorDeathHandler"/> drives via the shared DeathEvent bus (mirrors
/// <see cref="SiegeBossDeathHandler"/>/<see cref="VortexGeneratorDeathHandler"/>'s exact same shape).
///
/// Does not depend on <see cref="SiegeService"/> (a dependency the other direction already exists:
/// SiegeService constructs <see cref="Services.Siege.FortressSiege"/>, which needs this service injected —
/// see FortressSiege's own doc comment). Instead it operates directly on the <see cref="SiegeLocation"/>
/// passed to it and the generator <see cref="SiegeNpc"/> list <see cref="Services.Siege.FortressSiege"/>
/// already reads out of SiegeService's own registry after <c>SiegeService.SpawnNpcs</c> spawns them —
/// no NPC is spawned by this class itself, avoiding a double-spawn of the same siege-spawn templates.
///
/// note: Java's <em>other</em> shield mechanic — controllers.ShieldController/observer.ShieldObserver, a
/// geo-mesh sphere that outright kills an enemy-race player who crosses its boundary while the fortress is
/// under-shield — needs the knownlist "see"/movement-observer framework this port doesn't have yet (the
/// same gap already documented on <see cref="Model.Siege.SiegeShield"/> and referenced by
/// <see cref="SiegeOptions"/>'s own doc comment on the dead SIEGE_SHIELD_ENABLED Java config key).
/// <see cref="BlocksEntry"/> below exposes the equivalent predicate so a future phase can wire it into
/// whatever entry/teleport path lands first; nothing calls it yet.
///
/// Everything here is a no-op while <see cref="SiegeOptions.Enable"/> is false, matching every other
/// siege-broadcast/spawn path in this codebase.
/// </summary>
public sealed class ShieldService(
    PlayerConnectionRegistry connRegistry,
    IOptions<SiegeOptions> options,
    ILogger<ShieldService> log)
{
    /// <summary>Java <c>@AIName("siege_shieldnpc")</c> — the AI-script tag that marks an NPC's static
    /// template as a fortress shield generator (see <see cref="SpawnService.SpawnSiegeNpc"/>).</summary>
    public const string ShieldGeneratorAiName = "siege_shieldnpc";

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, byte>> _liveGeneratorsByLocationId = new();
    private readonly ConcurrentDictionary<int, SiegeLocation> _locationByGeneratorNpcId = new();

    /// <summary>
    /// Java ShieldNpcAI2.handleSpawned's fortress-level effect (generalized across every currently-alive
    /// generator — see this class's doc comment): raises the shield and starts tracking
    /// <paramref name="generatorNpcs"/> as the set whose death will drop it again. A no-op when the siege
    /// engine is disabled or no generator NPCs were spawned for this location (the latter also covers the
    /// data-not-shipped case — see <see cref="Services.SpawnService.SpawnSiegeNpc"/>'s Ai-tag check; the
    /// fortress simply starts un-shielded, faithfully mirroring Java's own behavior when no
    /// siege_shieldnpc-tagged spawn exists for a location).
    /// </summary>
    public async Task SpawnShieldAsync(SiegeLocation location, IReadOnlyCollection<SiegeNpc> generatorNpcs, CancellationToken ct = default)
    {
        if (!options.Value.Enable) return;

        if (generatorNpcs.Count == 0)
        {
            log.LogDebug("ShieldService: no shield-generator NPCs spawned for location {Id} — fortress starts un-shielded.",
                location.LocationId);
            return;
        }

        var liveIds = new ConcurrentDictionary<int, byte>();
        foreach (var generator in generatorNpcs)
        {
            liveIds[generator.Npc.ObjectId] = 0;
            _locationByGeneratorNpcId[generator.Npc.ObjectId] = location;
        }
        _liveGeneratorsByLocationId[location.LocationId] = liveIds;

        location.SetUnderShield(true);
        await BroadcastShieldAsync(location, ct);
    }

    /// <summary>
    /// Java ShieldNpcAI2.handleDespawned's fortress-level effect, triggered by a tracked generator's
    /// death (see <see cref="Combat.Handlers.ShieldGeneratorDeathHandler"/>). Drops the shield only once
    /// every generator registered for this fortress in <see cref="SpawnShieldAsync"/> is dead. A no-op for
    /// an NPC this service isn't tracking as a live generator.
    /// </summary>
    public async Task OnGeneratorDeathAsync(Npc npc, CancellationToken ct = default)
    {
        if (!_locationByGeneratorNpcId.TryRemove(npc.ObjectId, out var location)) return;
        if (!_liveGeneratorsByLocationId.TryGetValue(location.LocationId, out var liveIds)) return;

        liveIds.TryRemove(npc.ObjectId, out _);
        if (!liveIds.IsEmpty) return; // other generators are still alive — shield stays up

        _liveGeneratorsByLocationId.TryRemove(location.LocationId, out _);
        location.SetUnderShield(false);
        await BroadcastShieldAsync(location, ct);
    }

    /// <summary>Java FortressSiege.onSiegeFinish()'s unconditional <c>setUnderShield(false)</c> — called
    /// alongside it to purge this service's own generator bookkeeping so a stale entry from an
    /// interrupted siege (timed out or force-stopped with generators still alive) can't leak into the
    /// fortress's next siege cycle.</summary>
    public void ClearShield(int locationId)
    {
        if (!_liveGeneratorsByLocationId.TryRemove(locationId, out var liveIds)) return;
        foreach (var generatorId in liveIds.Keys)
            _locationByGeneratorNpcId.TryRemove(generatorId, out _);
    }

    /// <summary>Java controllers.ShieldController.see's core condition — true when an enemy-race player
    /// should be barred from a currently-shielded fortress. See this class's doc comment for why nothing
    /// wires this into an actual entry/movement path yet.</summary>
    public bool BlocksEntry(SiegeLocation location, Player player) =>
        location.IsUnderShield && location.Race != SiegeRaceExtensions.FromPlayerRace(player.Race);

    private async Task BroadcastShieldAsync(SiegeLocation location, CancellationToken ct)
    {
        if (!options.Value.Enable) return;

        AionServerPacket packet = new SM_SHIELD_EFFECT([location]);
        foreach (var conn in connRegistry.GetAll())
        {
            if (conn.ActivePlayer is not { } player || player.Position.WorldId != location.WorldId) continue;
            try { await conn.SendAsync(packet, ct); } catch { }
        }
    }
}
