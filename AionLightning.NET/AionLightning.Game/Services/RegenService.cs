using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Background service that ticks every 6 seconds and restores HP/MP for living players and NPCs.
/// Regen amounts: 2% of max per tick for players, 1% for NPCs.
/// </summary>
public sealed class RegenService : BackgroundService
{
    private static readonly TimeSpan Interval         = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan OutOfCombatDelay = TimeSpan.FromSeconds(5);

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GameWorld _world;
    private readonly ILogger<RegenService> _log;

    public RegenService(PlayerConnectionRegistry connRegistry, GameWorld world, ILogger<RegenService> log)
    {
        _connRegistry = connRegistry;
        _world        = world;
        _log          = log;
    }

    private async Task BroadcastGroupMemberUpdateAsync(Player player, CancellationToken ct)
    {
        var group = player.Group;
        if (group is null) return;

        var update = new SM_GROUP_MEMBER_INFO(group.GroupId, player, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
        foreach (var member in group.Members)
        {
            if (member.ObjectId == player.ObjectId) continue;
            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn is not null)
                await memberConn.SendAsync(update, ct);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("RegenService started (6-second tick)");
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await TickAsync(ct);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var conn in _connRegistry.GetAll())
        {
            var player = conn.ActivePlayer;
            if (player is null || player.IsAlreadyDead) continue;
            if (now - player.LastCombatTime < OutOfCombatDelay) continue;

            bool changed = false;

            int worldId = player.Position.WorldId;

            if (player.CurrentHp < player.MaxHp)
            {
                int regen   = Math.Max(1, player.MaxHp / 50);
                int actual  = Math.Min(regen, player.MaxHp - player.CurrentHp);
                player.CurrentHp += actual;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, 0, actual,
                    SM_ATTACK_STATUS.LogId.RegularHeal);
                await conn.SendAsync(pkt, ct);
                foreach (var peer in _connRegistry.GetAll())
                    if (peer != conn && peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                changed = true;
            }

            if (player.CurrentMp < player.MaxMp)
            {
                int regen   = Math.Max(1, player.MaxMp / 50);
                int actual  = Math.Min(regen, player.MaxMp - player.CurrentMp);
                player.CurrentMp += actual;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, 0, actual,
                    SM_ATTACK_STATUS.LogId.MpHeal);
                await conn.SendAsync(pkt, ct);
                foreach (var peer in _connRegistry.GetAll())
                    if (peer != conn && peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                changed = true;
            }

            if (changed)
                await BroadcastGroupMemberUpdateAsync(player, ct);
        }

        foreach (var npc in _world.GetAllNpcs())
        {
            if (npc.IsAlreadyDead || npc.CurrentHp >= npc.MaxHp) continue;
            if (now - npc.LastCombatTime < OutOfCombatDelay) continue;
            int regen = Math.Max(1, npc.MaxHp / 100);
            npc.CurrentHp = Math.Min(npc.MaxHp, npc.CurrentHp + regen);
        }
    }
}
