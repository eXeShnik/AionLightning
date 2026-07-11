using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Background service that ticks every 6 seconds and restores HP/MP for living players.
/// NPC HP regen is handled by NpcAiService (MaxHp/4 every 6s with SM_ATTACK_STATUS broadcast).
/// Player regen follows Java LifeStatsRestoreService: (level+3)*health/100 HP, (level+8)*will/100 MP per tick.
/// </summary>
public sealed class RegenService : BackgroundService
{
    private static readonly TimeSpan Interval         = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan OutOfCombatDelay = TimeSpan.FromSeconds(5);

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;
    private readonly ILogger<RegenService>    _log;

    public RegenService(PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ILogger<RegenService> log)
    {
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
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
                try { await memberConn.SendAsync(update, ct); } catch { }
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

    // FP drain per 6s tick while flying; regen per tick while grounded
    private const int FpDrainPerTick  = 75;
    private const int FpRegenPerTick  = 50;

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var conn in _connRegistry.GetAll())
        {
            var player = conn.ActivePlayer;
            if (player is null || player.IsAlreadyDead) continue;

            int worldId   = player.Position.WorldId;
            bool isFlying = player.State.HasFlag(CreatureState.Flying);

            // FP drain while flying (always — not gated by combat delay)
            if (isFlying && player.CurrentFp > 0)
            {
                player.CurrentFp = Math.Max(0, player.CurrentFp - FpDrainPerTick);
                try { await conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.EffectiveMaxFp), ct); } catch { }

                if (player.CurrentFp <= 0)
                {
                    // Force-land: clear Flying flag and broadcast the landing emotion
                    player.State &= ~CreatureState.Flying;
                    player.State &= ~CreatureState.Gliding;
                    var land = new SM_EMOTION(player, EmotionType.LAND);
                    try { await conn.SendAsync(land, ct); } catch { }
                    foreach (var peer in _connRegistry.GetAllExcept(player.ObjectId))
                        if (peer.ActivePlayer?.Position.WorldId == worldId)
                            try { await peer.SendAsync(land, ct); } catch { }
                }
            }

            // FP regen when grounded and out of combat
            if (!isFlying && player.CurrentFp < player.EffectiveMaxFp && now - player.LastCombatTime >= OutOfCombatDelay)
            {
                int fpRegen = player.BonusRegenFpPct != 0
                    ? Math.Max(1, FpRegenPerTick * (100 + player.BonusRegenFpPct) / 100)
                    : FpRegenPerTick;
                player.CurrentFp = Math.Min(player.EffectiveMaxFp, player.CurrentFp + fpRegen);
                try { await conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.EffectiveMaxFp), ct); } catch { }
            }

            if (now - player.LastCombatTime < OutOfCombatDelay) continue;

            // Java LifeStatsRestoreService: HP += (level+3)*health/100, MP += (level+8)*will/100 per 6s tick
            var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
            bool changed = false;

            if (player.CurrentHp < player.MaxHp)
            {
                int regen  = Math.Max(1, (player.Level + 3) * (statTpl?.Health ?? 100) / 100);
                if (player.BonusRegenHpPct  != 0) regen = Math.Max(1, regen * (100 + player.BonusRegenHpPct) / 100);
                if (player.BonusRegenHpFlat != 0) regen += player.BonusRegenHpFlat;
                int actual = Math.Min(regen, player.MaxHp - player.CurrentHp);
                player.CurrentHp += actual;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, 0, actual,
                    SM_ATTACK_STATUS.LogId.RegularHeal);
                await conn.SendAsync(pkt, ct);
                try { await conn.SendAsync(new SM_STATUPDATE_HP(player.CurrentHp, player.MaxHp), ct); } catch { }
                foreach (var peer in _connRegistry.GetAll())
                    if (peer != conn && peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                changed = true;
            }

            if (player.CurrentMp < player.MaxMp)
            {
                int regen  = Math.Max(1, (player.Level + 8) * (statTpl?.Will ?? 100) / 100);
                if (player.BonusRegenMpPct  != 0) regen = Math.Max(1, regen * (100 + player.BonusRegenMpPct) / 100);
                if (player.BonusRegenMpFlat != 0) regen += player.BonusRegenMpFlat;
                int actual = Math.Min(regen, player.MaxMp - player.CurrentMp);
                player.CurrentMp += actual;
                var pkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, 0, actual,
                    SM_ATTACK_STATUS.LogId.MpHeal);
                await conn.SendAsync(pkt, ct);
                try { await conn.SendAsync(new SM_STATUPDATE_MP(player.CurrentMp, player.MaxMp), ct); } catch { }
                foreach (var peer in _connRegistry.GetAll())
                    if (peer != conn && peer.ActivePlayer?.Position.WorldId == worldId)
                        try { await peer.SendAsync(pkt, ct); } catch { }
                changed = true;
            }

            if (changed)
                await BroadcastGroupMemberUpdateAsync(player, ct);
        }
    }
}
