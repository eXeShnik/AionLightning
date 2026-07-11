using AionLightning.Commons.Events;
using AionLightning.Game.Dao;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// PvP Phase 1 (2026-07-11 survey): centralizes the player-kills-player reward path that used to be
/// duplicated inline in CM_ATTACK.cs and CM_CASTSPELL.cs — AP exchange, rank update/broadcast, DAO
/// persistence, legion contribution, and (new) the kill_in_world quest notification.
/// </summary>
/// <remarks>
/// Deliberately NOT relocated here: the victim's own death packets (state flag, effect clear,
/// SM_EMOTION DIE, SM_ABNORMAL_EFFECT clear, SM_DIE + "you were killed by" to the victim). Those stay
/// inline at both call sites because <see cref="CreatureDamageExtensions.ApplyDamageAndPublishAsync"/>
/// publishes <see cref="DeathEvent"/> — and therefore invokes this handler — BEFORE the caller even
/// broadcasts the killing-blow SM_ATTACK/SM_ATTACK_STATUS packets, let alone reaches its own
/// "target died" branch. Moving the death-emotion/SM_DIE packets here would reorder them ahead of the
/// attack animation the client just played for the same hit. The reward/announce/persist block moved
/// here has no such ordering dependency: its packets (kill announcement, group-died notice, abyss
/// rank, legion) are independent of the attack animation sequence, matching this task's documented
/// fallback ("relocate only the reward/AP/announce/persist part and leave death packets inline").
/// </remarks>
public sealed class PvpKillHandler(
    PlayerConnectionRegistry connRegistry,
    IPlayerDao               playerDao,
    ILegionDao                legionDao,
    RateOptions               rates,
    DuelService               duelService,
    QuestEngineType           questEngine)
    : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Killer is not Player killer) return;
        if (e.Victim is not Player victim) return;

        // M379 interaction: ResurrectBaseHandler runs earlier in the DeathEvent handler chain and
        // may have already revived the victim (Chain of Suffering) — a revived "kill" isn't a kill.
        if (!victim.IsAlreadyDead) return;

        if (killer.Race == victim.Race) return;
        if (!AbyssRankService.IsPvPMap(killer.Position.WorldId)) return;

        // M287: duels suppress DeathEvent at the ApplyDamageAndPublishAsync call site already
        // (suppressDeathEvent: true); this is a defensive second check in case a future damage path
        // forgets to set that flag.
        if (duelService.GetOpponent(killer.ObjectId) == victim.ObjectId) return;

        int apGain = AbyssRankService.CalculatePvPApGained(killer, victim);
        if (rates.ApPlayerGainRate != 1.0f)
            apGain = Math.Max(1, (int)(apGain * rates.ApPlayerGainRate));
        if (killer.APBoostDelta != 0)
            apGain = Math.Max(1, apGain * (100 + killer.APBoostDelta) / 100);
        int apLoss = AbyssRankService.CalculatePvPApLost(killer, victim);
        bool killerRankUp = AbyssRankService.AddAp(killer, apGain);
        AbyssRankService.LoseAp(victim, apLoss);
        AbyssRankService.TrackPvPKill(killer, apGain);

        int worldId = killer.Position.WorldId;
        var killerConn = connRegistry.Get(killer.ObjectId);
        var victimConn = connRegistry.Get(victim.ObjectId);

        // Zone-wide kill announcement: "%0 was killed by %1's attack."
        var killAnnounce = SM_SYSTEM_MESSAGE.PlayerKilledByPlayer(victim.Name, killer.Name);
        foreach (var c in connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == worldId)
                try { await c.SendAsync(killAnnounce, ct); } catch { }

        // Group members see "[player] has died."
        var deadGroup = victim.Group;
        if (deadGroup is not null)
        {
            var groupDied = SM_SYSTEM_MESSAGE.GroupMemberDied(victim.Name);
            foreach (var m in deadGroup.Members)
            {
                if (m.ObjectId == victim.ObjectId) continue;
                var mc = connRegistry.Get(m.ObjectId);
                if (mc is not null) try { await mc.SendAsync(groupDied, ct); } catch { }
            }
        }

        if (killerConn is not null)
            try { await killerConn.SendAsync(SM_ABYSS_RANK.ForPlayer(killer), ct); } catch { }
        if (killerRankUp)
        {
            var rankPkt = new SM_ABYSS_RANK_UPDATE(killer.ObjectId, killer.AbyssRank);
            foreach (var c in connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == worldId)
                    try { await c.SendAsync(rankPkt, ct); } catch { }
        }
        if (victimConn is not null)
            try { await victimConn.SendAsync(SM_ABYSS_RANK.ForPlayer(victim), ct); } catch { }

        await playerDao.UpdateAbyssAsync(killer.ObjectId, killer.AbyssPoints, killer.AbyssRank, ct);
        await playerDao.UpdateAbyssAsync(victim.ObjectId, victim.AbyssPoints, victim.AbyssRank, ct);
        await playerDao.UpdateAbyssKillStatsAsync(killer.ObjectId,
            killer.AbyssAllKill, killer.AbyssMaxRank,
            killer.AbyssDailyKill, killer.AbyssDailyAp,
            killer.AbyssWeeklyKill, killer.AbyssWeeklyAp,
            killer.AbyssLastKill, killer.AbyssLastAp, ct);

        await AwardLegionContributionAsync(killer, apGain, ct);

        // PvP Phase 1: notify kill_in_world quests (Java PvpService.notifyKillQuests, minus the
        // group/alliance distribution — Phase 2, needs AggroList on Creature). Keyed by the
        // victim's world id (Java: `int worldId = victim.getWorldId();`).
        if (killerConn is not null)
            await questEngine.OnPlayerKillAsync(killer, victim, killerConn, ct);
    }

    private async ValueTask AwardLegionContributionAsync(Player player, long apAmount, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null || apAmount <= 0) return;

        legion.ContributionPoints += apAmount;
        await legionDao.UpdateContributionPointsAsync(legion.LegionId, legion.ContributionPoints, ct);

        var update = new SM_LEGION_EDIT(legion.ContributionPoints);
        foreach (var member in legion.Members.Values)
        {
            var mConn = connRegistry.Get(member.ObjectId);
            if (mConn is not null)
                try { await mConn.SendAsync(update, ct); } catch { }
        }
    }
}
