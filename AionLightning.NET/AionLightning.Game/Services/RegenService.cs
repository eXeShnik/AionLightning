using AionLightning.Game.Controllers;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Model;
using ZoneType = AionLightning.Game.Model.Zone.ZoneType;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Background service driving two independent tick cadences (Java LifeStatsRestoreService, which
/// schedules HP/MP restore at 6000ms and FP reduce/restore at 2000ms as separate tasks):
/// - Every 2s: FP drain/regen for flying/gliding/grounded players (Java FpReduceTask/FpRestoreTask),
///   plus a poll for glide-ending CC states (see FlyController.StopGlidingAsync doc).
/// - Every 3rd tick (6s): HP/MP restore for living players — (level+3)*health/100 HP,
///   (level+8)*will/100 MP per tick (Java LifeStatsRestoreService.HpMpRestoreTask).
/// NPC HP regen is handled separately by NpcAiService (MaxHp/4 every 6s with SM_ATTACK_STATUS broadcast).
/// </summary>
public sealed class RegenService : BackgroundService
{
    private static readonly TimeSpan FpTickInterval   = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan OutOfCombatDelay = TimeSpan.FromSeconds(5);
    private const int HpMpTicksPerFpTick = 3; // Java HP/MP restore (6000ms) runs every 3rd FP tick (2000ms)

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;
    private readonly ZoneService               _zoneService;
    private readonly FlyController              _flyController;
    private readonly ILogger<RegenService>    _log;

    public RegenService(PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ZoneService zoneService, FlyController flyController, ILogger<RegenService> log)
    {
        _connRegistry  = connRegistry;
        _dataManager   = dataManager;
        _zoneService   = zoneService;
        _flyController = flyController;
        _log           = log;
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
        _log.LogInformation("RegenService started (2-second FP tick; HP/MP every {N}rd tick)", HpMpTicksPerFpTick);
        using var timer = new PeriodicTimer(FpTickInterval);
        int tick = 0;
        while (await timer.WaitForNextTickAsync(ct))
        {
            await FpTickAsync(ct);
            if (++tick % HpMpTicksPerFpTick == 0)
                await HpMpTickAsync(ct);
        }
    }

    /// <summary>
    /// Java FpReduceTask/FpRestoreTask, folded into one 2s tick: drains FP while flying/gliding (1 per
    /// tick while gliding inside a FLY zone, else 2), force-lands at 0 FP, and restores 1 FP per tick
    /// while grounded. Also polls for glide-ending CC states (see FlyController.StopGlidingAsync doc).
    /// </summary>
    private async Task FpTickAsync(CancellationToken ct)
    {
        foreach (var conn in _connRegistry.GetAll())
        {
            var player = conn.ActivePlayer;
            if (player is null || player.IsAlreadyDead) continue;

            if (player.FlyState == 2 && (player.ActiveCcFlags & AbnormalCcFlags.CantMove) != 0)
                await _flyController.StopGlidingAsync(player, removeWings: true, conn, ct);

            if (player.FlyState > 0)
            {
                if (player.CurrentFp <= 0)
                {
                    await _flyController.EndFlyAsync(player, forceEndFly: true, conn, ct);
                    continue;
                }

                bool insideFlyZone = _zoneService.IsInsideZoneType(player, ZoneType.Fly);
                int reduceFp = player.FlyState == 2 && insideFlyZone ? 1 : 2;
                player.CurrentFp = Math.Max(0, player.CurrentFp - reduceFp);
                try { await conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.EffectiveMaxFp), ct); } catch { }
            }
            else if (player.CurrentFp < player.EffectiveMaxFp)
            {
                // Java FpRestoreTask.restoreFp: flat +1 FP per 2s tick while grounded
                int fpRegen = player.BonusRegenFpPct != 0
                    ? Math.Max(1, (100 + player.BonusRegenFpPct) / 100)
                    : 1;
                player.CurrentFp = Math.Min(player.EffectiveMaxFp, player.CurrentFp + fpRegen);
                try { await conn.SendAsync(new SM_FLY_TIME(player.CurrentFp, player.EffectiveMaxFp), ct); } catch { }
            }
        }
    }

    private async Task HpMpTickAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var conn in _connRegistry.GetAll())
        {
            var player = conn.ActivePlayer;
            if (player is null || player.IsAlreadyDead) continue;

            int worldId = player.Position.WorldId;

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
