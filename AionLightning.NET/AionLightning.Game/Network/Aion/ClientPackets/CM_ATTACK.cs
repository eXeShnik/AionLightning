using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_ATTACK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ExperienceService _expService;
    private readonly SpawnService _spawnService;
    private readonly LootService _lootService;
    private readonly QuestService _questService;
    private readonly DuelService _duelService;
    private readonly IPlayerDao _playerDao;
    private readonly ILegionDao _legionDao;
    private readonly RateOptions _rates;

    private int _targetObjectId;
    private int _time;

    public CM_ATTACK(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, ExperienceService expService,
        SpawnService spawnService, LootService lootService, QuestService questService,
        DuelService duelService, IPlayerDao playerDao, ILegionDao legionDao, RateOptions rates)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _expService   = expService;
        _spawnService = spawnService;
        _lootService  = lootService;
        _questService = questService;
        _duelService  = duelService;
        _playerDao    = playerDao;
        _legionDao    = legionDao;
        _rates        = rates;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        r.ReadC();           // attackno (unused)
        _time           = r.ReadH();
        r.ReadC();           // type (unused)
    }

    private const float MaxMeleeRange = 7.0f; // lenient for latency; retail is ~5m

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        // Enforce auto-attack cooldown (weapon attack speed; default 1500ms)
        var now = DateTime.UtcNow;
        if ((now - player.LastAttackTime).TotalMilliseconds < player.CurrentAttackSpeed) return;
        player.LastAttackTime = now;

        // Resolve target from world
        Creature? target = _world.GetPlayerByObjectId(_targetObjectId)
                        ?? (Creature?)_world.GetNpcByObjectId(_targetObjectId);
        if (target is null || target.IsAlreadyDead) return;

        // Target must be in same zone and within melee range
        if (target.Position.WorldId != player.Position.WorldId) return;
        if (player.Position.DistanceTo(target.Position) > MaxMeleeRange) return;

        // Physical damage: weapon range + base stat bonus, or stat-based fallback
        int baseAtk = player.BasePhysicalAttack > 0 ? player.BasePhysicalAttack : player.Level * 6;
        int rawDmg = player.MainHandMinDmg > 0
            ? Random.Shared.Next(player.MainHandMinDmg, Math.Max(player.MainHandMinDmg + 1, player.MainHandMaxDmg + 1)) + baseAtk
            : baseAtk + Random.Shared.Next(10, 40);

        // Apply physical defense mitigation from the target's armor
        int pdef = target is Player pvpTarget ? pvpTarget.PhysicalDefense
                 : target is Npc npcTarget    ? (npcTarget.Template.Stats?.PDef ?? 0)
                 : 0;
        int damage = pdef > 0 ? Math.Max(1, rawDmg * 1000 / (1000 + pdef)) : rawDmg;
        target.CurrentHp = Math.Max(0, target.CurrentHp - damage);

        // Both attacker and target enter combat — suppresses regen for both
        var combatNow = now;
        player.LastCombatTime = combatNow;
        target.LastCombatTime = combatNow;

        // Broadcast attack animation then damage report
        await BroadcastAsync(new SM_ATTACK(player, target, attackno: 0, time: (short)_time, type: 0, damage), ct);
        await BroadcastAsync(new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, damage), ct);

        // DP gain on successful physical hit (100 DP per attack, capped at 6000)
        if (player.Dp < 6000)
        {
            player.Dp = Math.Min(6000, player.Dp + 100);
            try { await _conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp), ct); } catch { }
        }

        // Update group HP display for player targets (PvP)
        if (target is Player damagedPlayer)
        {
            var grp = damagedPlayer.Group;
            if (grp is not null)
            {
                var update = new SM_GROUP_MEMBER_INFO(grp.GroupId, damagedPlayer, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
                foreach (var m in grp.Members)
                {
                    if (m.ObjectId == damagedPlayer.ObjectId) continue;
                    var mc = _connRegistry.Get(m.ObjectId);
                    if (mc is not null) try { await mc.SendAsync(update, ct); } catch { }
                }
            }
        }

        if (target.CurrentHp > 0) return;

        // Target died
        if (target is Player deadPlayer)
        {
            // Duel: end without killing — restore 1 HP, send result, clear duel state
            if (_duelService.GetOpponent(player.ObjectId) == deadPlayer.ObjectId)
            {
                deadPlayer.CurrentHp = 1;
                _duelService.EndDuel(player.ObjectId, deadPlayer.ObjectId);
                await _conn.SendAsync(SM_DUEL.Won(deadPlayer.Name), ct);
                var loserConn = _connRegistry.Get(deadPlayer.ObjectId);
                if (loserConn is not null)
                    try { await loserConn.SendAsync(SM_DUEL.Lost(player.Name), ct); } catch { }
                return;
            }

            deadPlayer.State |= CreatureState.Dead;
            await BroadcastAsync(new SM_EMOTION(deadPlayer, EmotionType.DIE), ct);

            var targetConn = _connRegistry.Get(deadPlayer.ObjectId);
            if (targetConn is not null)
                try { await targetConn.SendAsync(new SM_DIE(), ct); } catch { }

            // PvP AP exchange — only in Abyss/Balaurea maps AND opposing factions
            if (AbyssRankService.IsPvPMap(player.Position.WorldId) && player.Race != deadPlayer.Race)
            {
                int apGain = AbyssRankService.CalculatePvPApGained(player, deadPlayer);
                if (_rates.ApPlayerGainRate != 1.0f)
                    apGain = Math.Max(1, (int)(apGain * _rates.ApPlayerGainRate));
                int apLoss = AbyssRankService.CalculatePvPApLost(player, deadPlayer);
                AbyssRankService.AddAp(player, apGain);
                AbyssRankService.LoseAp(deadPlayer, apLoss);

                await _conn.SendAsync(new SM_ABYSS_RANK(player.AbyssPoints, player.AbyssRank), ct);
                if (targetConn is not null)
                    try { await targetConn.SendAsync(new SM_ABYSS_RANK(deadPlayer.AbyssPoints, deadPlayer.AbyssRank), ct); } catch { }

                await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, ct);
                await _playerDao.UpdateAbyssAsync(deadPlayer.ObjectId, deadPlayer.AbyssPoints, deadPlayer.AbyssRank, ct);
                await AwardLegionContributionAsync(player, apGain, ct);
            }
        }
        else if (target is Npc deadNpc)
        {
            deadNpc.State |= CreatureState.Dead;
            await BroadcastAsync(new SM_EMOTION(deadNpc, EmotionType.DIE), ct);
            _world.Remove(deadNpc);

            // Generate drops keyed by the NPC's objectId (now removed from world)
            _lootService.GenerateDrops(deadNpc);

            // Update quest kill progress for active quests
            await _questService.HandleNpcKillAsync(player, deadNpc, _conn, ct);

            // Award XP — use template value when available, fallback to level-based estimate
            long xpReward = deadNpc.Template.Stats?.MaxXp > 0
                ? deadNpc.Template.Stats.MaxXp
                : deadNpc.Level * 50L;
            await _expService.AddGroupExpAsync(player, xpReward, ct);

            // Award AP for kills in the Abyss world or against ABYSS_GUARD NPCs; persist immediately
            if (deadNpc.Position.WorldId == AbyssRankService.AbyssWorldId
                || deadNpc.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase))
            {
                int ap = AbyssRankService.CalculateNpcApReward(deadNpc.Level);
                AbyssRankService.AddAp(player, ap);
                await _conn.SendAsync(new SM_ABYSS_RANK(player.AbyssPoints, player.AbyssRank), ct);
                await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, ct);
                await AwardLegionContributionAsync(player, ap, ct);
            }

            // Delayed despawn + respawn; also cleans up uncollected loot after 60s
            var registry  = _connRegistry;
            var spawnSvc  = _spawnService;
            var lootSvc   = _lootService;
            int npcWorldId = deadNpc.Position.WorldId;
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                var del = new SM_DELETE(deadNpc.ObjectId);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                        try { await c.SendAsync(del); } catch { }
                spawnSvc.ScheduleRespawn(deadNpc);

                await Task.Delay(57_000); // 60s total from kill
                lootSvc.ClearLoot(deadNpc.ObjectId);
            });
        }
    }

    private async ValueTask BroadcastAsync(AionServerPacket packet, CancellationToken ct)
    {
        try { await _conn.SendAsync(packet, ct); } catch { }
        int worldId = _conn.ActivePlayer!.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(_conn.ActivePlayer!.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(packet, ct); } catch { }
    }

    private async ValueTask AwardLegionContributionAsync(Model.Player player, long apAmount, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null || apAmount <= 0) return;

        legion.ContributionPoints += apAmount;
        await _legionDao.UpdateContributionPointsAsync(legion.LegionId, legion.ContributionPoints, ct);

        var update = new SM_LEGION_EDIT(legion.ContributionPoints);
        foreach (var member in legion.Members.Values)
        {
            var mConn = _connRegistry.Get(member.ObjectId);
            if (mConn is not null)
                try { await mConn.SendAsync(update, ct); } catch { }
        }
    }
}
