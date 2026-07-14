using System.Collections.Concurrent;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Java <c>services.KiskService</c> — bind stones ("kisks"). A player deploys a kisk item to spawn a
/// kisk NPC (see <see cref="SpawnKiskAsync"/>, wired from CM_USE_ITEM's kisk-deploy branch); eligible
/// players (same race, and per <see cref="Kisk.UseMask"/> same legion/group/alliance/solo) bind to it
/// (<see cref="OnBindAsync"/>), which becomes their revive point on death until the kisk runs out of
/// resurrects or its 2-hour lifetime expires (<see cref="RemoveKiskAsync"/>).
/// Owns two registries: which kisk each placer currently has deployed (one-kisk-per-owner, mirroring
/// Java's <c>CustomConfig.ENABLE_KISK_RESTRICTION</c> — hardcoded on here since this port has no
/// equivalent config surface), and which <see cref="Kisk"/> wraps a given kisk NPC's objectId (so
/// scripted AI — see Scripts/ai/KiskAI2.cs — can resolve back to the wrapper via
/// <see cref="NpcAi2"/>'s static-injected accessor).
/// note: Java also kept a boundButOfflinePlayer map so a bind survived a relog within the kisk's
/// lifetime (restored via KiskService.onLogin/onLogout, called from PlayerEnterWorldService /
/// PlayerLeaveWorldService). This port has no player-logout hook at all yet (see Player.Kisk's doc
/// comment), so that map has no way to be populated here — relogging while bound simply loses the bind
/// (falls back to the player's obelisk bind point on death) instead of restoring it.
/// </summary>
public sealed class KiskService
{
    private readonly IDataManager _dataManager;
    private readonly GameWorld _world;
    private readonly SpawnService _spawnService;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<KiskService> _log;

    // Owner (placer) objectId -> their one active kisk.
    private readonly ConcurrentDictionary<int, Kisk> _ownerPlayer = new();
    // Kisk NPC objectId -> its Kisk wrapper (lets scripted AI resolve Npc -> Kisk; see NpcAi2.GetKisk).
    private readonly ConcurrentDictionary<int, Kisk> _byNpcObjectId = new();

    public KiskService(IDataManager dataManager, GameWorld world, SpawnService spawnService,
        PlayerConnectionRegistry connRegistry, ILogger<KiskService> log)
    {
        _dataManager  = dataManager;
        _world        = world;
        _spawnService = spawnService;
        _connRegistry = connRegistry;
        _log          = log;
    }

    /// <summary>Java KiskService.haveKisk.</summary>
    public bool HasKisk(int ownerObjectId) => _ownerPlayer.ContainsKey(ownerObjectId);

    /// <summary>Resolves a spawned NPC back to the Kisk wrapper it belongs to, or null when
    /// <paramref name="npc"/> isn't a registered kisk.</summary>
    public Kisk? GetByNpc(Npc npc) => _byNpcObjectId.GetValueOrDefault(npc.ObjectId);

    /// <summary>
    /// Java ToyPetSpawnAction.act + VisibleObjectSpawner.spawnKisk: spawns a kisk NPC from
    /// <paramref name="npcId"/> at <paramref name="player"/>'s position (heading offset by 60°, matching
    /// Java's <c>(heading + 60) % 120</c>), registers it, schedules its 2-hour despawn, and auto-binds
    /// the placer. Returns null (and notifies the player) when they already have an active kisk or the
    /// npcId has no template.
    /// note: Java opened a bind-confirmation dialog (SM_QUESTION_WINDOW via AI2Request) for the placer
    /// when the kisk's max-members &gt; 1, only auto-binding solo (1-member) kisks immediately; that
    /// dialog round-trip isn't wired at the script layer yet (see KiskAI2.OnDialogStart's note), so this
    /// port always auto-binds the placer immediately regardless of tier.
    /// </summary>
    public async ValueTask<Kisk?> SpawnKiskAsync(Player player, int npcId, CancellationToken ct = default)
    {
        if (HasKisk(player.ObjectId))
        {
            var ownerConn = _connRegistry.Get(player.ObjectId);
            if (ownerConn is not null)
                try { await ownerConn.SendAsync(SM_SYSTEM_MESSAGE.BindstoneAlreadyInstalled(), ct); } catch { }
            return null;
        }

        var template = _dataManager.Npcs.GetTemplate(npcId);
        if (template is null)
        {
            _log.LogWarning("KiskService: no NPC template for kisk npcId {NpcId}", npcId);
            return null;
        }

        byte heading = (byte)((player.Position.Heading + 60) % 120);
        var pos = player.Position with { Heading = heading };
        var npc = _spawnService.SpawnNpcAt(template, pos);

        var kisk = new Kisk(npc, player, template.KiskStats);
        _ownerPlayer[player.ObjectId] = kisk;
        _byNpcObjectId[npc.ObjectId]  = kisk;

        var infoPacket = new SM_NPC_INFO(npc);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer is { } peer && peer.Position.SameScope(pos))
                try { await conn.SendAsync(infoPacket, ct); } catch { }

        ScheduleDespawn(kisk);

        await OnBindAsync(kisk, player, ct);
        return kisk;
    }

    private void ScheduleDespawn(Kisk kisk)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(Kisk.LifetimeSeconds));
            if (_byNpcObjectId.ContainsKey(kisk.Npc.ObjectId))
                await RemoveKiskAsync(kisk);
        });
    }

    /// <summary>
    /// Java Kisk.canBind: does <paramref name="player"/> qualify to bind to <paramref name="kisk"/>?
    /// The placer themselves always qualifies; everyone else is gated by <see cref="Kisk.UseMask"/>
    /// (1/0=race, 2=legion, 3=solo-only, 4=group, 5=alliance) and the member-count cap.
    /// note: Java also checked SerialKillerService.isRestrictDynamicBindstone (PK-rank players can be
    /// barred from dynamic bindstones) — no serial-killer/PK-rank system is ported yet, so that gate is
    /// skipped here.
    /// </summary>
    public bool CanBind(Kisk kisk, Player player)
    {
        if (player.ObjectId != kisk.OwnerId)
        {
            switch (kisk.UseMask)
            {
                case 0:
                case 1: // race
                    if (player.Race != kisk.OwnerRace) return false;
                    break;
                case 2: // legion
                    if (kisk.OwnerLegionId is not int legionId || player.Legion?.LegionId != legionId) return false;
                    break;
                case 3: // solo — only the placer (already checked above)
                    return false;
                case 4: // group
                    if (player.Group is null || !player.Group.HasMember(kisk.OwnerId)) return false;
                    break;
                case 5: // alliance
                    if (player.Alliance is null || !player.Alliance.HasMember(kisk.OwnerId)) return false;
                    break;
                default:
                    return false;
            }
        }

        return kisk.MemberIds.Count < kisk.MaxMembers || kisk.MemberIds.Contains(player.ObjectId);
    }

    /// <summary>
    /// Java KiskService.onBind: binds <paramref name="player"/> to <paramref name="kisk"/> (unbinding
    /// from any previous kisk first), sets their revive point to it, and notifies them.
    /// </summary>
    public async ValueTask OnBindAsync(Kisk kisk, Player player, CancellationToken ct = default)
    {
        if (player.Kisk is { } previous && previous != kisk)
            UnbindFrom(previous, player);

        kisk.MemberIds.Add(player.ObjectId);
        player.Kisk = kisk;

        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is not null)
        {
            try { await conn.SendAsync(new SM_BIND_POINT_INFO(kisk.Npc.Position, kisk), ct); } catch { }
            try { await conn.SendAsync(SM_SYSTEM_MESSAGE.BindstoneRegister(), ct); } catch { }
        }

        await BroadcastKiskUpdateAsync(kisk, ct);
    }

    private static void UnbindFrom(Kisk kisk, Player player)
    {
        kisk.MemberIds.Remove(player.ObjectId);
        player.Kisk = null;
    }

    private async ValueTask BroadcastKiskUpdateAsync(Kisk kisk, CancellationToken ct)
    {
        var packet = new SM_KISK_UPDATE(kisk);
        foreach (int memberId in kisk.MemberIds)
        {
            var conn = _connRegistry.Get(memberId);
            if (conn is not null) try { await conn.SendAsync(packet, ct); } catch { }
        }
    }

    /// <summary>
    /// Java KiskService.removeKisk: despawns the kisk NPC and unbinds every member — clearing their
    /// bind-point display back to obelisk-only and, if they're still lying dead, re-sending SM_DIE so
    /// the client drops the now-invalid kisk-revive option.
    /// </summary>
    public async ValueTask RemoveKiskAsync(Kisk kisk, CancellationToken ct = default)
    {
        _ownerPlayer.TryRemove(kisk.OwnerId, out _);
        _byNpcObjectId.TryRemove(kisk.Npc.ObjectId, out _);

        foreach (int memberId in kisk.MemberIds.ToArray())
        {
            var member = _world.GetPlayerByObjectId(memberId);
            if (member is null) continue; // offline member — nothing to unbind server-side (see class doc)
            member.Kisk = null;

            var conn = _connRegistry.Get(memberId);
            if (conn is null) continue;
            try { await conn.SendAsync(new SM_BIND_POINT_INFO(member.BindPosition ?? default), ct); } catch { }
            if (member.IsAlreadyDead)
                try { await conn.SendAsync(new SM_DIE(), ct); } catch { }
        }
        kisk.MemberIds.Clear();

        var scope = kisk.Npc.Position;
        _world.Remove(kisk.Npc);
        var deletePacket = new SM_DELETE(kisk.Npc.ObjectId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer is { } peer && peer.Position.SameScope(scope))
                try { await conn.SendAsync(deletePacket, ct); } catch { }
    }

    /// <summary>
    /// Java PlayerReviveService.kiskRevive's kisk-side half, called from CM_REVIVE's kisk-revive case:
    /// consumes one of <paramref name="player"/>'s bound kisk's resurrects and returns the position to
    /// revive at, or null when they have no active kisk binding (caller should fall back to a plain
    /// bind revive). Despawning-on-last-resurrect mirrors Java's <c>Kisk.resurrectionUsed()</c> calling
    /// <c>onDelete()</c> at zero.
    /// </summary>
    public async ValueTask<Position?> ConsumeResurrectionAsync(Player player, CancellationToken ct = default)
    {
        var kisk = player.Kisk;
        if (kisk is null || !kisk.IsActive) return null;

        var bindPosition = kisk.Npc.Position;
        kisk.RemainingResurrects--;
        await BroadcastKiskUpdateAsync(kisk, ct);

        if (kisk.RemainingResurrects <= 0)
            await RemoveKiskAsync(kisk, ct);

        return bindPosition;
    }
}
