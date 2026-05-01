using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Background service that ticks every 2 seconds, handles NPC aggro/combat and idle wander.
/// Aggressive NPCs (AggroRange > 0, Ai != "dummy") engage nearby players; idle NPCs wander
/// within WanderRadius units of their spawn point.
/// NPCs chase targets: move toward the player when outside melee range, attack when within range.
/// </summary>
public sealed class NpcAiService : BackgroundService
{
    private static readonly TimeSpan Interval              = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DefaultAttackCooldown = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan WanderCooldown        = TimeSpan.FromSeconds(10);
    private const float LeashMultiplier   = 1.5f;
    private const float WanderRadius      = 5.0f;
    private const float WanderSpeed       = 1.5f;
    private const float ChaseSpeed        = 6.0f;
    private const float MeleeRange        = 2.5f;
    private const int   NpcSkillCooldownMs = 8000;

    private sealed record WanderState(float Tx, float Ty, float Tz, DateTime ArrivalTime);
    private sealed record ChaseState(float Tx, float Ty, float Tz, DateTime ArrivalTime);
    private sealed record ReturnState(float Tx, float Ty, float Tz, DateTime ArrivalTime);

    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;
    private readonly ILogger<NpcAiService> _log;
    private readonly RateOptions _rates;
    private readonly Dictionary<int, DateTime>    _lastAttackTime  = new();
    private readonly Dictionary<int, DateTime>    _lastSkillTime   = new();
    private readonly Dictionary<int, int>         _npcTargets      = new();
    private readonly Dictionary<int, WanderState> _wanderState     = new();
    private readonly Dictionary<int, DateTime>    _lastWanderTime  = new();
    private readonly Dictionary<int, ChaseState>  _chaseState      = new();
    private readonly Dictionary<int, ReturnState> _returnState     = new();
    // Walker patrol: NPC objectId → current route step index
    private readonly Dictionary<int, int>         _walkerStepIndex = new();

    public NpcAiService(GameWorld world, PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ILogger<NpcAiService> log, IOptions<RateOptions> rates)
    {
        _world        = world;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
        _log          = log;
        _rates        = rates.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("NpcAiService started (2-second tick)");
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await TickAsync(ct);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var players = _world.GetAll().ToList();
        if (players.Count == 0)
        {
            _npcTargets.Clear();
            _lastAttackTime.Clear();
            _lastSkillTime.Clear();
            _chaseState.Clear();
            _returnState.Clear();
            return;
        }

        foreach (var npc in _world.GetAllNpcs())
        {
            if (npc.IsAlreadyDead)
            {
                _lastAttackTime.Remove(npc.ObjectId);
                _lastSkillTime.Remove(npc.ObjectId);
                _npcTargets.Remove(npc.ObjectId);
                _wanderState.Remove(npc.ObjectId);
                _lastWanderTime.Remove(npc.ObjectId);
                _chaseState.Remove(npc.ObjectId);
                _returnState.Remove(npc.ObjectId);
                _walkerStepIndex.Remove(npc.ObjectId);
                npc.Target = null;
                continue;
            }
            bool isDummy = string.Equals(npc.Template.Ai, "dummy", StringComparison.OrdinalIgnoreCase);

            // If returning home, snap position on arrival; skip all AI until home
            if (_returnState.TryGetValue(npc.ObjectId, out var rs))
            {
                if (DateTime.UtcNow >= rs.ArrivalTime)
                {
                    npc.Position = npc.Position with { X = rs.Tx, Y = rs.Ty, Z = rs.Tz };
                    _returnState.Remove(npc.ObjectId);
                }
                else
                {
                    continue; // still returning — no aggro, no wander this tick
                }
            }

            // Aggro + combat
            Player? target = null;
            if (!isDummy && npc.Template.AggroRange > 0)
            {
                float leashRange = npc.Template.AggroRange * LeashMultiplier;

                // Validate locked target — clear if dead, wrong world, or out of leash range
                if (_npcTargets.TryGetValue(npc.ObjectId, out int lockedId))
                {
                    var locked = players.FirstOrDefault(p => p.ObjectId == lockedId);
                    if (locked is not null
                        && !locked.IsAlreadyDead
                        && locked.Position.WorldId == npc.HomePosition.WorldId
                        && npc.HomePosition.DistanceTo(locked.Position) <= leashRange)
                    {
                        target = locked;
                    }
                    else
                    {
                        // Lost target — stop any chase and return home
                        await StopChaseAsync(npc, ct);
                        _npcTargets.Remove(npc.ObjectId);
                        npc.Target = null;
                    }
                }

                // No locked target — scan from NPC's current position for nearest player in aggro range
                if (target is null)
                {
                    float minDist = float.MaxValue;
                    foreach (var player in players)
                    {
                        if (player.IsAlreadyDead) continue;
                        if (player.Position.WorldId != npc.Position.WorldId) continue;
                        if (!_dataManager.Tribes.IsAggressiveToPlayer(npc.Template.Tribe, player.Race)) continue;

                        float dist = npc.Position.DistanceTo(player.Position);
                        if (dist <= npc.Template.AggroRange && dist < minDist)
                        {
                            minDist = dist;
                            target  = player;
                        }
                    }
                    if (target is not null)
                    {
                        _npcTargets[npc.ObjectId] = target.ObjectId;
                        npc.Target = target;

                        // SEE shout — NPC has just spotted a player
                        var shout = _dataManager.NpcShouts.GetRandomShout(
                            npc.Template.NpcId, NpcShoutData.ShoutEventType.SEE, npc.Position.WorldId);
                        if (shout.HasValue)
                        {
                            var shoutPkt  = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, shout.Value.StringId);
                            int shoutWorld = npc.Position.WorldId;
                            foreach (var conn in _connRegistry.GetAll())
                                if (conn.ActivePlayer?.Position.WorldId == shoutWorld)
                                    try { await conn.SendAsync(shoutPkt, ct); } catch { }
                        }
                    }
                }
            }

            // Wander when idle (no aggro target and not a dummy)
            if (target is null && !isDummy)
                await WanderAsync(npc, ct);

            if (target is null) continue;

            // Advance any ongoing chase — update NPC position on arrival
            if (_chaseState.TryGetValue(npc.ObjectId, out var cs) && DateTime.UtcNow >= cs.ArrivalTime)
            {
                npc.Position = npc.Position with { X = cs.Tx, Y = cs.Ty, Z = cs.Tz };
                _chaseState.Remove(npc.ObjectId);
            }

            float distToTarget = npc.Position.DistanceTo(target.Position);

            // Outside melee range — chase the target
            if (distToTarget > MeleeRange)
            {
                await ChaseAsync(npc, target, ct);
                continue; // don't attack this tick while chasing
            }

            // Within melee range — stop any chase animation, then attack
            if (_chaseState.ContainsKey(npc.ObjectId))
            {
                var stopPkt = SM_MOVE.StopNpcMove(npc.ObjectId,
                    npc.Position.X, npc.Position.Y, npc.Position.Z, (byte)npc.Position.Heading);
                int stopWorld = npc.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == stopWorld)
                        try { await c.SendAsync(stopPkt, ct); } catch { }
                _chaseState.Remove(npc.ObjectId);
            }

            var now = DateTime.UtcNow;
            int atkDelayMs = npc.Template.AttackDelay > 0 ? npc.Template.AttackDelay : (int)DefaultAttackCooldown.TotalMilliseconds;
            if ((now - _lastAttackTime.GetValueOrDefault(npc.ObjectId)).TotalMilliseconds < atkDelayMs) continue;
            _lastAttackTime[npc.ObjectId] = now;

            // Deal damage — both NPC and target enter combat (suppresses regen for both)
            int baseAtk = npc.Template.Stats?.MainHandAttack ?? 0;
            int rawDmg  = baseAtk > 0
                ? Math.Max(1, baseAtk + Random.Shared.Next(-(baseAtk / 4), baseAtk / 4 + 1))
                : Math.Max(1, npc.Level * 5 + Random.Shared.Next(5, 20));
            if (_rates.NormalMobsRatePw != 1.0)
                rawDmg = Math.Max(1, (int)(rawDmg * _rates.NormalMobsRatePw));

            // Apply physical defense mitigation (diminishing returns: pdef / (pdef + 1000))
            int pdef   = target is Player tp ? tp.PhysicalDefense : 0;
            int damage = pdef > 0 ? Math.Max(1, rawDmg * 1000 / (1000 + pdef)) : rawDmg;

            target.CurrentHp      = Math.Max(0, target.CurrentHp - damage);
            target.LastCombatTime = now;
            npc.LastCombatTime    = now;

            var attackPkt = new SM_ATTACK(npc, target, attackno: 0, time: 0, type: 0, damage);
            var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, damage);
            int npcWorld  = npc.Position.WorldId;
            foreach (var conn in _connRegistry.GetAll())
            {
                if (conn.ActivePlayer?.Position.WorldId != npcWorld) continue;
                try { await conn.SendAsync(attackPkt, ct); } catch { /* ignore */ }
                try { await conn.SendAsync(statusPkt, ct); } catch { /* ignore */ }
            }

            await BroadcastGroupHpAsync(target, ct);

            // Occasional NPC skill use (independent of melee cooldown)
            if (target.CurrentHp > 0)
                await TryCastNpcSkillAsync(npc, target, now, npcWorld, ct);

            if (target.CurrentHp > 0) continue;

            // Player killed by NPC — clear their target lock so NPC idles afterward
            _npcTargets.Remove(npc.ObjectId);
            _chaseState.Remove(npc.ObjectId);
            _lastSkillTime.Remove(npc.ObjectId);
            npc.Target = null;

            target.State |= CreatureState.Dead;
            var diePkt = new SM_EMOTION(target, EmotionType.DIE);
            foreach (var conn in _connRegistry.GetAll())
            {
                if (conn.ActivePlayer?.Position.WorldId != npcWorld) continue;
                try { await conn.SendAsync(diePkt, ct); } catch { /* ignore */ }
            }

            var targetConn = _connRegistry.Get(target.ObjectId);
            if (targetConn is not null)
                try { await targetConn.SendAsync(new SM_DIE(), ct); } catch { /* ignore */ }
        }
    }

    private async Task TryCastNpcSkillAsync(Npc npc, Player target, DateTime now, int worldId, CancellationToken ct)
    {
        if ((now - _lastSkillTime.GetValueOrDefault(npc.ObjectId)).TotalMilliseconds < NpcSkillCooldownMs) return;

        var skills = _dataManager.NpcSkills.GetSkills(npc.Template.NpcId);
        if (skills is null || skills.Count == 0) return;

        var entry = skills[Random.Shared.Next(skills.Count)];
        if (Random.Shared.Next(100) >= entry.Probability) return;

        _lastSkillTime[npc.ObjectId] = now;

        var castPkt = new SM_CASTSPELL(npc.ObjectId, entry.SkillId, entry.SkillLevel, 3, target.ObjectId, 0);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(castPkt, ct); } catch { }

        int rawSpellDmg = Math.Max(1, npc.Level * 8 + Random.Shared.Next(10, 40));
        int mdef        = target.MagicDefense;
        int spellDmg    = mdef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + mdef)) : rawSpellDmg;
        target.CurrentHp      = Math.Max(0, target.CurrentHp - spellDmg);
        target.LastCombatTime = now;
        npc.LastCombatTime    = now;

        var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, entry.SkillId, spellDmg);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(statusPkt, ct); } catch { }
    }

    private async Task ChaseAsync(Npc npc, Player target, CancellationToken ct)
    {
        float dx = target.Position.X - npc.Position.X;
        float dy = target.Position.Y - npc.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        // Destination: stop just inside melee range
        float stopDist = Math.Max(0, dist - MeleeRange * 0.8f);
        float tx = npc.Position.X + dx / dist * stopDist;
        float ty = npc.Position.Y + dy / dist * stopDist;
        float tz = target.Position.Z;

        float travel  = MathF.Sqrt((tx - npc.Position.X) * (tx - npc.Position.X)
                                  + (ty - npc.Position.Y) * (ty - npc.Position.Y));
        double seconds = travel / ChaseSpeed;
        var arrival   = DateTime.UtcNow.AddSeconds(seconds < 0.1 ? 0.1 : seconds);

        byte heading  = CalcHeading(dx, dy);
        _chaseState[npc.ObjectId] = new ChaseState(tx, ty, tz, arrival);

        var movePkt = SM_MOVE.StartNpcMove(npc.ObjectId,
            npc.Position.X, npc.Position.Y, npc.Position.Z, heading, tx, ty, tz);
        int worldId = npc.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(movePkt, ct); } catch { }
    }

    private async Task StopChaseAsync(Npc npc, CancellationToken ct)
    {
        if (!_chaseState.ContainsKey(npc.ObjectId)) return;
        _chaseState.Remove(npc.ObjectId);

        float dx   = npc.HomePosition.X - npc.Position.X;
        float dy   = npc.HomePosition.Y - npc.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.5f) return;

        byte heading  = CalcHeading(dx, dy);
        double seconds = dist / ChaseSpeed;
        var arrival   = DateTime.UtcNow.AddSeconds(seconds);

        // Track the return so TickAsync snaps position on arrival (no instant snap)
        _returnState[npc.ObjectId] = new ReturnState(
            npc.HomePosition.X, npc.HomePosition.Y, npc.HomePosition.Z, arrival);

        var returnPkt = SM_MOVE.StartNpcMove(npc.ObjectId,
            npc.Position.X, npc.Position.Y, npc.Position.Z, heading,
            npc.HomePosition.X, npc.HomePosition.Y, npc.HomePosition.Z);
        int worldId = npc.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(returnPkt, ct); } catch { }
    }

    private Task WanderAsync(Npc npc, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(npc.WalkerId))
        {
            var route = _dataManager.Walkers.GetRoute(npc.WalkerId);
            if (route is { Length: > 0 })
                return PatrolAsync(npc, route, ct);
        }
        return WanderRandomAsync(npc, ct);
    }

    private async Task PatrolAsync(Npc npc, WalkerData.RouteStep[] route, CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        int worldId = npc.Position.WorldId;

        // Check if an in-progress wander move has arrived
        if (_wanderState.TryGetValue(npc.ObjectId, out var ws))
        {
            if (now >= ws.ArrivalTime)
            {
                npc.Position = npc.Position with { X = ws.Tx, Y = ws.Ty, Z = ws.Tz };
                var stop = SM_MOVE.StopNpcMove(npc.ObjectId, ws.Tx, ws.Ty, ws.Tz, (byte)npc.Position.Heading);
                _wanderState.Remove(npc.ObjectId);
                // Advance to next step (cycle)
                int prev = _walkerStepIndex.GetValueOrDefault(npc.ObjectId, 0);
                _walkerStepIndex[npc.ObjectId] = (prev + 1) % route.Length;
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(stop, ct); } catch { }
            }
            return; // still traveling
        }

        // Start moving to the current step
        int stepIdx = _walkerStepIndex.GetValueOrDefault(npc.ObjectId, 0);
        var step    = route[stepIdx % route.Length];

        float dx = step.X - npc.Position.X;
        float dy = step.Y - npc.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.5f)
        {
            // Already at this step — advance immediately without broadcasting a zero-distance move
            _walkerStepIndex[npc.ObjectId] = (stepIdx + 1) % route.Length;
            return;
        }

        float travelTime = dist / WanderSpeed;
        var   arrival    = now.AddSeconds(travelTime < 0.5 ? 0.5 : travelTime);
        byte  heading    = CalcHeading(dx, dy);

        _wanderState[npc.ObjectId] = new WanderState(step.X, step.Y, step.Z, arrival);

        var movePkt = SM_MOVE.StartNpcMove(npc.ObjectId,
            npc.Position.X, npc.Position.Y, npc.Position.Z, heading, step.X, step.Y, step.Z);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(movePkt, ct); } catch { }
    }

    private async Task WanderRandomAsync(Npc npc, CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        int worldId = npc.HomePosition.WorldId;

        // If there's an active wander, check arrival
        if (_wanderState.TryGetValue(npc.ObjectId, out var ws))
        {
            if (now >= ws.ArrivalTime)
            {
                // Arrived — update NPC position and broadcast stop
                npc.Position = npc.Position with { X = ws.Tx, Y = ws.Ty, Z = ws.Tz };
                var stop = SM_MOVE.StopNpcMove(npc.ObjectId, ws.Tx, ws.Ty, ws.Tz, (byte)npc.Position.Heading);
                _wanderState.Remove(npc.ObjectId);
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(stop, ct); } catch { }
            }
            return; // still traveling, or just arrived (done for this tick)
        }

        // Cool-down before picking a new wander target
        if (now - _lastWanderTime.GetValueOrDefault(npc.ObjectId) < WanderCooldown) return;

        // Pick a random destination within WanderRadius of HomePosition
        float angle = (float)(Random.Shared.NextDouble() * 2 * Math.PI);
        float dist  = (float)(Random.Shared.NextDouble() * WanderRadius);
        float tx    = npc.HomePosition.X + MathF.Cos(angle) * dist;
        float ty    = npc.HomePosition.Y + MathF.Sin(angle) * dist;
        float tz    = npc.HomePosition.Z;

        float travel  = MathF.Sqrt((tx - npc.Position.X) * (tx - npc.Position.X)
                                  + (ty - npc.Position.Y) * (ty - npc.Position.Y));
        float seconds = travel / WanderSpeed;
        var arrival   = now.AddSeconds(seconds < 0.5 ? 0.5 : seconds);

        byte heading  = CalcHeading(tx - npc.Position.X, ty - npc.Position.Y);

        _wanderState[npc.ObjectId]    = new WanderState(tx, ty, tz, arrival);
        _lastWanderTime[npc.ObjectId] = now;

        var start = SM_MOVE.StartNpcMove(npc.ObjectId, npc.Position.X, npc.Position.Y, npc.Position.Z,
            heading, tx, ty, tz);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(start, ct); } catch { }
    }

    private static byte CalcHeading(float dx, float dy)
        => (byte)((int)(MathF.Atan2(dy, dx) * (128.0f / MathF.PI) + 64) & 0xFF);

    private async Task BroadcastGroupHpAsync(Player target, CancellationToken ct)
    {
        var group = target.Group;
        if (group is null) return;
        var update = new SM_GROUP_MEMBER_INFO(group.GroupId, target, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
        foreach (var member in group.Members)
        {
            if (member.ObjectId == target.ObjectId) continue;
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(update, ct); } catch { /* ignore */ }
        }
    }
}
