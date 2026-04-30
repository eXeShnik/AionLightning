using AionLightning.Commons.Network;
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

    private int _targetObjectId;
    private int _time;

    public CM_ATTACK(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, ExperienceService expService,
        SpawnService spawnService, LootService lootService, QuestService questService,
        DuelService duelService)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _expService   = expService;
        _spawnService = spawnService;
        _lootService  = lootService;
        _questService = questService;
        _duelService  = duelService;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        r.ReadC();           // attackno (unused)
        _time           = r.ReadH();
        r.ReadC();           // type (unused)
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        // Resolve target from world
        Creature? target = _world.GetPlayerByObjectId(_targetObjectId)
                        ?? (Creature?)_world.GetNpcByObjectId(_targetObjectId);
        if (target is null || target.IsAlreadyDead) return;

        // Placeholder physical damage: level-scaled with small random spread
        int damage = player.Level * 6 + Random.Shared.Next(10, 40);
        target.CurrentHp = Math.Max(0, target.CurrentHp - damage);

        // Both attacker and target enter combat — suppresses regen for both
        var combatNow = DateTime.UtcNow;
        player.LastCombatTime = combatNow;
        target.LastCombatTime = combatNow;

        // Broadcast attack animation then damage report
        await BroadcastAsync(new SM_ATTACK(player, target, attackno: 0, time: (short)_time, type: 0, damage), ct);
        await BroadcastAsync(new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, damage), ct);

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
                await targetConn.SendAsync(new SM_DIE(), ct);
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

            // Award XP — split among group members if in a party
            long xpReward = deadNpc.Level * 50L;
            await _expService.AddGroupExpAsync(player, xpReward, ct);

            // Award AP for kills in the Abyss world or against ABYSS_GUARD NPCs
            if (deadNpc.Position.WorldId == AbyssRankService.AbyssWorldId
                || deadNpc.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase))
            {
                int ap = AbyssRankService.CalculateNpcApReward(deadNpc.Level);
                AbyssRankService.AddAp(player, ap);
                await _conn.SendAsync(new SM_ABYSS_RANK(player.AbyssPoints, player.AbyssRank), ct);
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
}
