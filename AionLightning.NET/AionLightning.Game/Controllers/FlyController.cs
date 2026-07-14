using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Controllers;

/// <summary>
/// Java <c>controllers.FlyController</c> — Java holds one instance per Player; this port keeps a
/// single DI-singleton controller (matches <see cref="HouseController"/>'s convention) that drives
/// flight state directly on the <see cref="Player"/> model instead of a per-object controller
/// hierarchy. Owns the fly-state machine (<see cref="Player.FlyState"/>: 0 = grounded, 1 = flying,
/// 2 = gliding) and the FLY_REUSE_TIME (10s) anti-hack cooldown shared by starting flight and
/// switching to glide.
/// note: Java's trailing <c>updateStatsAndSpeedVisually()</c> call (recomputes fly vs. run speed and
/// pushes SM_STATS_INFO) has no equivalent hook in this port yet — no unified stats-refresh service
/// exists for MovementSpeed/BonusFlySpeedPct recomputation on fly-state transitions.
/// </summary>
public sealed class FlyController(PlayerConnectionRegistry connRegistry)
{
    private const long FlyReuseTimeMs = 10_000;

    /// <summary>
    /// Java FlyController.startFly — called by CM_EMOTION's FLY case once the zone/no-fly gate passes.
    /// Returns false (no state change, no broadcast) when the FLY_REUSE_TIME anti-hack cooldown is
    /// still active, matching Java's early-return (Java additionally calls AuditLogger there; no audit
    /// log exists in this port, so the hack attempt is silently ignored).
    /// </summary>
    public async ValueTask<bool> StartFlyAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (player.FlyReuseTime > now) return false;

        player.FlyReuseTime = now + FlyReuseTimeMs;
        player.State |= CreatureState.Flying;
        // note: Java also sets FLOATING_CORPSE here when isInPlayerMode(PlayerMode.RIDE) — no mount/ride
        // system is ported yet, so CreatureState.FloatingCorpse (added for parity) is never entered from here.
        player.FlyState = 1;

        await BroadcastAsync(player, EmotionType.START_EMOTE2, conn, ct);
        return true;
    }

    /// <summary>
    /// Java FlyController.endFly — ends both flying and gliding. Called from CM_EMOTION's LAND case
    /// (forceEndFly=false) and from RegenService's 0-FP force-land path (forceEndFly=true).
    /// note: LAND_FLYTELEPORT is a distinct emotion in Java — it calls
    /// <c>PlayerController.onFlyTeleportEnd</c> instead (ported as
    /// <see cref="Services.TeleportService.OnFlyTeleportEndAsync"/>), not this method.
    /// </summary>
    public async ValueTask EndFlyAsync(Player player, bool forceEndFly, GsClientConnection conn, CancellationToken ct)
    {
        if (!player.State.HasFlag(CreatureState.Flying) && !player.State.HasFlag(CreatureState.Gliding))
            return;

        player.State &= ~CreatureState.Flying;
        player.State &= ~CreatureState.Gliding;
        player.State &= ~CreatureState.FloatingCorpse;
        player.FlyState = 0;

        await BroadcastAsync(player, EmotionType.START_EMOTE2, conn, ct);
        if (forceEndFly)
            await BroadcastAsync(player, EmotionType.LAND, conn, ct);
    }

    /// <summary>
    /// Java FlyController.switchToGliding — switches into glide mode, either from standing (starts FP
    /// drain via FlyState leaving 0) or from flying (FlyState 1 -> 2, drain rate only).
    /// note: nothing calls this yet — CM_MOVE does not parse the GLIDE movement-mask bit (see its Read
    /// method's "GLIDE and VEHICLE flags skipped in M5" comment), so VALIDATE_GLIDE never reaches here.
    /// Wired for when that packet parsing is added.
    /// </summary>
    public bool SwitchToGliding(Player player)
    {
        if (player.State.HasFlag(CreatureState.Gliding)) return true;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (player.FlyReuseTime > now) return false;

        player.FlyReuseTime = now + FlyReuseTimeMs;
        player.State |= CreatureState.Gliding;
        player.FlyState = 2;
        return true;
    }

    /// <summary>
    /// Java FlyController.onStopGliding — ends only the glide sub-state: drops back to plain flying
    /// (FlyState 1) if still airborne, or fully lands (FlyState 0) if not.
    /// note: Java arms this via a glideObserver registered on ObserveController that fires the instant a
    /// CANT_MOVE_STATE debuff (stun/root/sleep/...) lands on the player, and additionally guards on
    /// <c>!player.isInvulnerableWing()</c> (no such wing-invulnerability flag exists in this port). No
    /// push-based effect-application observer exists here either, so RegenService's 2s FP tick polls
    /// <see cref="Creature.ActiveCcFlags"/> for CantMove on every gliding player and calls this — same
    /// end state as Java, just up to ~2s late instead of instant.
    /// </summary>
    public async ValueTask StopGlidingAsync(Player player, bool removeWings, GsClientConnection conn, CancellationToken ct)
    {
        if (!player.State.HasFlag(CreatureState.Gliding)) return;

        player.State &= ~CreatureState.Gliding;
        player.FlyState = player.State.HasFlag(CreatureState.Flying) ? 1 : 0;

        if (player.FlyState == 0 && removeWings)
            await BroadcastAsync(player, EmotionType.LAND, conn, ct);
    }

    private async ValueTask BroadcastAsync(Player player, EmotionType type, GsClientConnection conn, CancellationToken ct)
    {
        var pkt = new SM_EMOTION(player, type);
        try { await conn.SendAsync(pkt, ct); } catch { }
        int worldId = player.Position.WorldId;
        foreach (var peer in connRegistry.GetAllExcept(player.ObjectId))
            if (peer.ActivePlayer?.Position.WorldId == worldId)
                try { await peer.SendAsync(pkt, ct); } catch { }
    }
}
