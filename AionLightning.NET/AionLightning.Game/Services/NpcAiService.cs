using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Background service that ticks every 2 seconds and makes aggressive NPCs attack nearby players.
/// NPCs with AggroRange > 0 and Ai != "dummy" will engage and lock onto the nearest player in range.
/// Once engaged, the NPC stays locked on its target until the target dies, leaves leash range, or
/// the NPC dies. Leash range = 1.5× AggroRange measured from the NPC's spawn (HomePosition).
/// </summary>
public sealed class NpcAiService : BackgroundService
{
    private static readonly TimeSpan Interval       = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AttackCooldown = TimeSpan.FromSeconds(4);
    private const float LeashMultiplier = 1.5f;

    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<NpcAiService> _log;
    private readonly Dictionary<int, DateTime> _lastAttackTime = new();
    private readonly Dictionary<int, int>      _npcTargets     = new(); // npcObjectId → locked playerObjectId

    public NpcAiService(GameWorld world, PlayerConnectionRegistry connRegistry, ILogger<NpcAiService> log)
    {
        _world        = world;
        _connRegistry = connRegistry;
        _log          = log;
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
            return;
        }

        foreach (var npc in _world.GetAllNpcs())
        {
            if (npc.IsAlreadyDead)
            {
                _lastAttackTime.Remove(npc.ObjectId);
                _npcTargets.Remove(npc.ObjectId);
                continue;
            }
            if (npc.Template.AggroRange <= 0) continue;
            if (string.Equals(npc.Template.Ai, "dummy", StringComparison.OrdinalIgnoreCase)) continue;

            float leashRange = npc.Template.AggroRange * LeashMultiplier;

            // Validate locked target — clear if dead, wrong world, or out of leash range
            Player? target = null;
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
                    _npcTargets.Remove(npc.ObjectId);
                }
            }

            // No locked target — scan for nearest player inside aggro range
            if (target is null)
            {
                float minDist = float.MaxValue;
                foreach (var player in players)
                {
                    if (player.IsAlreadyDead) continue;
                    if (player.Position.WorldId != npc.HomePosition.WorldId) continue;

                    float dist = npc.HomePosition.DistanceTo(player.Position);
                    if (dist <= npc.Template.AggroRange && dist < minDist)
                    {
                        minDist = dist;
                        target  = player;
                    }
                }
                if (target is not null)
                    _npcTargets[npc.ObjectId] = target.ObjectId;
            }

            if (target is null) continue;

            var now = DateTime.UtcNow;
            if (now - _lastAttackTime.GetValueOrDefault(npc.ObjectId) < AttackCooldown) continue;
            _lastAttackTime[npc.ObjectId] = now;

            // Deal damage — both NPC and target enter combat (suppresses regen for both)
            int damage = npc.Level * 5 + Random.Shared.Next(5, 20);
            target.CurrentHp      = Math.Max(0, target.CurrentHp - damage);
            target.LastCombatTime = now;
            npc.LastCombatTime    = now;

            var attackPkt = new SM_ATTACK(npc, target, attackno: 0, time: 0, type: 0, damage);
            var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, damage);
            int npcWorld  = npc.HomePosition.WorldId;
            foreach (var conn in _connRegistry.GetAll())
            {
                if (conn.ActivePlayer?.Position.WorldId != npcWorld) continue;
                try { await conn.SendAsync(attackPkt, ct); } catch { /* ignore */ }
                try { await conn.SendAsync(statusPkt, ct); } catch { /* ignore */ }
            }

            await BroadcastGroupHpAsync(target, ct);

            if (target.CurrentHp > 0) continue;

            // Player killed by NPC — clear their target lock so NPC idles afterward
            _npcTargets.Remove(npc.ObjectId);

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
