using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.FlyPath;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// The single choke point for moving a player to a new <c>(worldId, instanceId)</c> position
/// (Java <c>TeleportService2</c>). Encapsulates the proven departure/arrival packet sequence that
/// was previously duplicated inline in the flight-master and instance-leave packet handlers, and
/// fires the instance enter/leave hooks on a scope change.
/// </summary>
public sealed class TeleportService
{
    private const int TeleportDelayMs = 2200;

    /// <summary>Java TeleportService2.teleport's FLIGHT-branch distance gate (hardcoded <c>7</c>).</summary>
    private const float FlyPathStartTolerance = 7f;

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;
    private readonly InstanceService _instanceService;
    private readonly ZoneService _zoneService;
    private readonly WeatherService _weatherService;
    private readonly ILogger<TeleportService> _log;

    public TeleportService(PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        InstanceService instanceService, ZoneService zoneService, WeatherService weatherService,
        ILogger<TeleportService> log)
    {
        _connRegistry    = connRegistry;
        _dataManager     = dataManager;
        _instanceService = instanceService;
        _zoneService     = zoneService;
        _weatherService  = weatherService;
        _log             = log;
    }

    /// <summary>
    /// Starts a flight-master glide teleport (Java <c>TeleportService2.teleport</c>'s <c>TeleportType.FLIGHT</c>
    /// branch). <paramref name="flyPathId"/> is the flypath_template id, which is the same id as the
    /// FLIGHT-type &lt;telelocation&gt; loc_id offering the destination (see <see cref="TeleportData.GetDestination"/>).
    /// Validates the player is standing within <see cref="FlyPathStartTolerance"/> of the path's start
    /// coordinates and in the path's native start world (Java's <c>SecurityConfig.ENABLE_FLYPATH_VALIDATOR</c>
    /// gate — no such config toggle exists in this port, so the check always runs). On success, marks
    /// <see cref="CreatureState.FlightTeleport"/>, stamps <see cref="Player.CurrentFlyPath"/> /
    /// <see cref="Player.FlyStartTime"/>, and broadcasts SM_EMOTION START_FLYTELEPORT — the client then
    /// glides on its own and reports position via CM_MOVE_IN_AIR, arriving at
    /// <see cref="OnFlyTeleportEndAsync"/> via CM_EMOTION LAND_FLYTELEPORT.
    /// note: Java also calls <c>player.unsetPlayerMode(PlayerMode.RIDE)</c> here — no mount/ride system
    /// is ported yet, so there is nothing to unset.
    /// </summary>
    public async ValueTask<bool> FlyTeleportAsync(Player player, short flyPathId, CancellationToken ct = default)
    {
        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is null) return false;

        var flyPath = _dataManager.FlyPaths.GetPathTemplate(flyPathId);
        if (flyPath is null)
        {
            _log.LogWarning("Player {Name} tried to use null flyPath #{Id}", player.Name, flyPathId);
            await SendNoRouteAsync(conn, ct);
            return false;
        }

        var here = player.Position;
        float dist = here.DistanceTo(here with { X = flyPath.StartX, Y = flyPath.StartY, Z = flyPath.StartZ });
        if (dist > FlyPathStartTolerance)
        {
            _log.LogWarning("Player {Name} tried to use flyPath #{Id} but is too far ({Dist:F1})", player.Name, flyPathId, dist);
            await SendNoRouteAsync(conn, ct);
            return false;
        }

        if (here.WorldId != flyPath.StartWorldId)
        {
            _log.LogWarning("Player {Name} tried to use flyPath #{Id} from non-native start world {World}; expected {Expected}",
                player.Name, flyPathId, here.WorldId, flyPath.StartWorldId);
            await SendNoRouteAsync(conn, ct);
            return false;
        }

        player.CurrentFlyPath = flyPath;
        player.FlyStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        player.State |= CreatureState.FlightTeleport;
        player.State &= ~CreatureState.Active;
        player.FlightTeleportId = flyPathId;

        var packet = new SM_EMOTION(player, EmotionType.START_FLYTELEPORT, flyPathId);
        try { await conn.SendAsync(packet, ct); } catch { }
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer is { } op && op.Position.SameScope(player.Position))
                try { await other.SendAsync(packet, ct); } catch { }

        return true;
    }

    /// <summary>
    /// Java <c>PlayerController.onFlyTeleportEnd</c>'s non-windstream branch (no windstream/mount mode
    /// is ported, so that branch never applies here) — called from CM_EMOTION's LAND_FLYTELEPORT case.
    /// Clears the flight-teleport state, runs the arrival-side anti-bug validator (world match + elapsed
    /// time), and refreshes the player's zone membership. Java's validator only logs suspicious arrivals
    /// via AuditLogger — the "todo if works teleport player to start_* xyz, or even ban" branch was never
    /// implemented in the source, so this port faithfully only logs (via <see cref="ILogger"/>, since no
    /// AuditLogger exists in this port) and does not force-correct the player's position.
    /// </summary>
    public async ValueTask OnFlyTeleportEndAsync(Player player, CancellationToken ct = default)
    {
        player.State &= ~CreatureState.FlightTeleport;
        player.State |= CreatureState.Active;
        player.FlightTeleportId = 0;

        FlyPathEntry? path = player.CurrentFlyPath;
        if (path is not null)
        {
            long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - player.FlyStartTime;

            if (player.Position.WorldId != path.EndWorldId)
                _log.LogWarning("Player {Name} used flyPath #{Id} but ended in world {World}; expected {Expected}",
                    player.Name, path.Id, player.Position.WorldId, path.EndWorldId);

            if (elapsed < path.TimeMs)
                _log.LogWarning("Player {Name} used flypath bug: arrived in {Elapsed}ms instead of {Expected}ms on flyPath #{Id}",
                    player.Name, elapsed, path.TimeMs, path.Id);

            player.CurrentFlyPath = null;
        }

        player.FlightDistance = 0;

        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is not null)
            await _zoneService.UpdateZonesAsync(player, conn, ct);
    }

    private static async ValueTask SendNoRouteAsync(GsClientConnection conn, CancellationToken ct)
    {
        try { await conn.SendAsync(SM_SYSTEM_MESSAGE.CannotMoveToAirportNoRoute(), ct); } catch { }
    }

    /// <summary>
    /// Moves <paramref name="player"/> to the given world/channel/coords. Notifies old-scope peers of
    /// departure, updates the server position, sends the client teleport command, and schedules the
    /// post-animation spawn packets. Fires <see cref="InstanceService.OnLeaveInstance"/> /
    /// <see cref="InstanceService.OnEnterInstance"/> when the instance scope changes.
    /// </summary>
    public ValueTask TeleportToAsync(Player player, int worldId, int instanceId,
        float x, float y, float z, byte heading = 0, byte portAnimation = 0, CancellationToken ct = default)
    {
        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is null) return ValueTask.CompletedTask;

        var oldScope = player.Position;

        // Departure: jump-animation SM_DELETE to everyone who could see the player at the old scope.
        var deletePacket = new SM_DELETE(player.ObjectId, time: 11);
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer is { } op && op.Position.SameScope(oldScope))
                try { _ = other.SendAsync(deletePacket, ct); } catch { }

        // Instance leave hook (before the position moves away from the old channel).
        if (oldScope.InstanceId != 0)
            _instanceService.OnLeaveInstance(player, oldScope);

        player.Position = new Position(x, y, z, heading, worldId, instanceId);

        // Instance enter hook (now inside the new channel).
        if (instanceId != 0)
            _instanceService.OnEnterInstance(player);

        _ = conn.SendAsync(new SM_TELEPORT_LOC(worldId, x, y, z, heading, portAnimation, instanceId), ct);
        _ = SchedulePostTeleportAsync(player, oldScope, ct);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Evicts a player from an instance to its race-specific exit location (Java
    /// <c>moveToInstanceExit</c>). No-op if the player is not in a world with a defined exit.
    /// </summary>
    public ValueTask MoveToInstanceExitAsync(Player player, CancellationToken ct = default)
    {
        var exit = _dataManager.InstanceExits.GetExit(player.Position.WorldId, player.Race);
        if (exit is null) return ValueTask.CompletedTask;
        return TeleportToAsync(player, exit.Value.ExitWorldId, 0,
            exit.Value.X, exit.Value.Y, exit.Value.Z, (byte)exit.Value.Heading, portAnimation: 0, ct);
    }

    private async Task SchedulePostTeleportAsync(Player player, Position oldScope, CancellationToken ct)
    {
        var conn = _connRegistry.Get(player.ObjectId);
        if (conn is null) return;
        try
        {
            await Task.Delay(TeleportDelayMs, ct);

            if (oldScope.WorldId != player.Position.WorldId || oldScope.InstanceId != player.Position.InstanceId)
            {
                // Cross-scope: full zone reload. CM_LEVEL_READY from the client re-introduces peers/NPCs.
                await conn.SendAsync(new SM_CHANNEL_INFO(), ct);
                await conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);

                // Java WeatherService.loadWeather(player) — re-sent on every world change (not just
                // initial login), so this stays outside the byte-verified PlayerEnterWorldService sequence.
                await _weatherService.SendWeatherAsync(player, conn, ct);
            }
            else
            {
                // Same-scope reposition: re-sync self, re-broadcast to same-scope peers.
                var equipment = player.Inventory.All.Where(i => i.IsEquipped).ToList();
                await conn.SendAsync(new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment), ct);
                await conn.SendAsync(new SM_STATS_INFO(player), ct);
                await conn.SendAsync(SM_MOTION.OwnList(player.ActiveMotions), ct);

                var scope    = player.Position;
                var peerInfo = new SM_PLAYER_INFO(player, player.Appearance, enemy: false, equipment);
                foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                    if (other.ActivePlayer is { } op && op.Position.SameScope(scope))
                        try { await other.SendAsync(peerInfo, ct); } catch { }
            }
        }
        catch (OperationCanceledException) { }
    }
}
