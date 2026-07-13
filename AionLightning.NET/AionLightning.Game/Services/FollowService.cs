using System.Collections.Concurrent;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Services;

/// <summary>
/// Drives quest escort/follow NPCs (Java <c>defaultStartFollowEvent</c> + <c>QuestTasks
/// .FollowingToTargetCheckTask</c>). A registered follower NPC walks toward its owning player each
/// tick; when it reaches its destination (fixed coords, a target NPC's spawn, or a named zone) the
/// quest's <c>onNpcReachTarget</c> fires, and if the player gets too far away <c>onNpcLostTarget</c>
/// fires. Movement is speed-limited so a walking player is kept up with but the escort can still be
/// lost if the player runs far ahead.
/// </summary>
public sealed class FollowService : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
    private const float FollowStopDistance = 3f;   // stop this far behind the player
    private const float ReachDistance      = 6f;   // destination considered reached within this
    private const float LostDistance       = 120f; // player this far from the follower ⇒ lost
    private const float FollowSpeed        = 7f;   // m/s (slightly faster than a walking player)

    private sealed record Escort(Npc Follower, Player Player, int QuestId, GsClientConnection Conn, Func<Position, bool> Reached);

    private readonly ConcurrentDictionary<int, Escort> _escorts = new(); // keyed by follower objectId
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly QuestEngineType _questEngine;
    private readonly IDataManager _dataManager;
    private readonly ILogger<FollowService> _log;

    public FollowService(PlayerConnectionRegistry connRegistry, QuestEngineType questEngine,
        IDataManager dataManager, ILogger<FollowService> log)
    {
        _connRegistry = connRegistry;
        _questEngine  = questEngine;
        _dataManager  = dataManager;
        _log          = log;
    }

    /// <summary>Escort until the follower is within reach of fixed coordinates.</summary>
    public void StartFollowToCoords(Npc follower, Player player, int questId, GsClientConnection conn, float x, float y, float z)
        => Start(follower, player, questId, conn, pos => Dist(pos, x, y, z) <= ReachDistance);

    /// <summary>Escort until the follower reaches the spawn location of <paramref name="targetNpcId"/>
    /// (Java resolves the target NPC's spawn spot up-front). Falls back to a never-reached escort
    /// (lost-only) if that NPC has no static spawn.</summary>
    public void StartFollowToNpc(Npc follower, Player player, int questId, GsClientConnection conn, int targetNpcId)
    {
        var spawn = _dataManager.Spawns.GetFirstSpawnByNpcId(targetNpcId);
        if (spawn is { } s)
            StartFollowToCoords(follower, player, questId, conn, s.Spot.X, s.Spot.Y, s.Spot.Z);
        else
            Start(follower, player, questId, conn, _ => false);
    }

    /// <summary>Escort until the follower is inside the named zone region.</summary>
    public void StartFollowToZone(Npc follower, Player player, int questId, GsClientConnection conn, string zoneName)
        => Start(follower, player, questId, conn, pos => IsInZone(pos, zoneName));

    private void Start(Npc follower, Player player, int questId, GsClientConnection conn, Func<Position, bool> reached)
    {
        try { _ = conn.SendAsync(new SM_NPC_INFO(follower), CancellationToken.None); } catch { }
        _escorts[follower.ObjectId] = new Escort(follower, player, questId, conn, reached);
    }

    /// <summary>Cancels an active escort for a follower (e.g. the quest advanced/abandoned).</summary>
    public void Stop(int followerObjectId) => _escorts.TryRemove(followerObjectId, out _);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Tick);
        while (await timer.WaitForNextTickAsync(ct))
        {
            foreach (var (id, escort) in _escorts)
            {
                try { await StepAsync(id, escort, ct); }
                catch (Exception ex) { _log.LogError(ex, "FollowService step failed for follower {Id}", id); }
            }
        }
    }

    private async Task StepAsync(int id, Escort e, CancellationToken ct)
    {
        // Drop the escort if the player is no longer online.
        if (_connRegistry.Get(e.Player.ObjectId) is null) { _escorts.TryRemove(id, out _); return; }

        var follower = e.Follower;
        var player   = e.Player;

        // Walk toward the player, capped at FollowSpeed per tick, stopping FollowStopDistance behind.
        float dx = player.Position.X - follower.Position.X;
        float dy = player.Position.Y - follower.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist > FollowStopDistance)
        {
            float step = MathF.Min(FollowSpeed, dist - FollowStopDistance);
            float tx = follower.Position.X + dx / dist * step;
            float ty = follower.Position.Y + dy / dist * step;
            float tz = player.Position.Z;
            byte heading = (byte)(MathF.Atan2(dy, dx) / (2 * MathF.PI) * 120);

            var startPos = follower.Position;
            var move = SM_MOVE.StartNpcMove(follower.ObjectId, startPos.X, startPos.Y, startPos.Z, heading, tx, ty, tz);
            follower.Position = startPos with { X = tx, Y = ty, Z = tz, Heading = heading };
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } p && p.Position.SameScope(follower.Position))
                    try { await conn.SendAsync(move, ct); } catch { }
        }

        // Reached the destination?
        if (e.Reached(follower.Position))
        {
            _escorts.TryRemove(id, out _);
            await _questEngine.OnNpcReachTargetAsync(new QuestEnv(follower, player, e.QuestId, 0), e.Conn, ct);
            return;
        }

        // Lost the player?
        if (follower.Position.DistanceTo(player.Position) > LostDistance)
        {
            _escorts.TryRemove(id, out _);
            await _questEngine.OnNpcLostTargetAsync(new QuestEnv(follower, player, e.QuestId, 0), e.Conn, ct);
        }
    }

    private static float Dist(Position pos, float x, float y, float z)
    {
        float dx = pos.X - x, dy = pos.Y - y, dz = pos.Z - z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private bool IsInZone(Position pos, string zoneName)
    {
        foreach (var region in _dataManager.Zones.GetRegionsForWorld(pos.WorldId))
            if (string.Equals(region.Name, zoneName, StringComparison.OrdinalIgnoreCase) && region.IsInside(pos.X, pos.Y, pos.Z))
                return true;
        return false;
    }
}
