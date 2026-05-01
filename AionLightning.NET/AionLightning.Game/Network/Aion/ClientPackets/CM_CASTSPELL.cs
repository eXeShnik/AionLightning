using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CASTSPELL : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;
    private readonly ExperienceService _expService;
    private readonly SpawnService _spawnService;
    private readonly LootService _lootService;
    private readonly QuestService _questService;
    private readonly DuelService _duelService;
    private readonly IPlayerDao _playerDao;
    private readonly ILegionDao _legionDao;
    private readonly RateOptions _rates;

    private int _spellId;
    private int _level;
    private int _targetType;
    private int _targetObjectId;
    private float _x, _y, _z;
    private int _hitTime;

    public CM_CASTSPELL(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ExperienceService expService, SpawnService spawnService, LootService lootService,
        QuestService questService, DuelService duelService, IPlayerDao playerDao, ILegionDao legionDao, RateOptions rates)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
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
        _spellId    = r.ReadH();
        _level      = r.ReadC();
        _targetType = r.ReadC();

        switch (_targetType)
        {
            case 0:
            case 3:
            case 4:
                _targetObjectId = r.ReadD();
                break;
            case 1:
                _x = r.ReadF(); _y = r.ReadF(); _z = r.ReadF();
                break;
            case 2:
                _x = r.ReadF(); _y = r.ReadF(); _z = r.ReadF();
                for (int i = 0; i < 8; i++) r.ReadF();
                break;
        }

        _hitTime = r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        if (!player.Skills.IsPresent(_spellId)) return;

        // Resolve template early to check cooldown
        var template = _dataManager.Skills.GetTemplate(_spellId);

        // Server-side cooldown enforcement (client enforces display; server enforces rules)
        var skillEntry = player.Skills.GetEntry(_spellId);
        if (skillEntry is not null && template is not null && skillEntry.IsOnCooldown(template.Cooldown))
            return;

        // Start cooldown immediately on cast attempt
        skillEntry?.MarkUsed();

        // Broadcast cast animation
        var castPacket = _targetType is 1 or 2
            ? new SM_CASTSPELL(player.ObjectId, _spellId, _level, _targetType, _x, _y, _z, _hitTime)
            : new SM_CASTSPELL(player.ObjectId, _spellId, _level, _targetType, _targetObjectId, _hitTime);

        await BroadcastAsync(castPacket, ct);
        bool isHealSkill = template?.SubType is SkillSubType.HEAL;
        bool isDamageSkill = !isHealSkill
            && template?.SkillType is SkillType.MAGICAL or SkillType.PHYSICAL
            && template.SubType is not (SkillSubType.BUFF or SkillSubType.CHANT);
        int castDelay = template?.Duration ?? 0;
        float castRange = template?.CastRange ?? 0f;

        if (isHealSkill && _targetType is 0 or 3 or 4)
        {
            // Heal target — self if targetObjectId == 0 or is the caster
            var healTarget = (_targetObjectId == 0 || _targetObjectId == player.ObjectId)
                ? (Creature)player
                : (Creature?)_world.GetPlayerByObjectId(_targetObjectId);
            if (healTarget is not null && !healTarget.IsAlreadyDead)
            {
                int heal = player.Level * 6 + Random.Shared.Next(15, 40);
                healTarget.CurrentHp = Math.Min(healTarget.MaxHp, healTarget.CurrentHp + heal);
                var healStatus = new SM_ATTACK_STATUS(healTarget, SM_ATTACK_STATUS.AttackType.NaturalHp,
                    _spellId, heal, SM_ATTACK_STATUS.LogId.Heal);
                int healWorldId = player.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == healWorldId)
                        try { await c.SendAsync(healStatus, ct); } catch { }

                // Update group HP display for healed player
                if (healTarget is Player healedPlayer)
                {
                    var grp = healedPlayer.Group;
                    if (grp is not null)
                    {
                        var update = new SM_GROUP_MEMBER_INFO(grp.GroupId, healedPlayer, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
                        foreach (var m in grp.Members)
                        {
                            if (m.ObjectId == healedPlayer.ObjectId) continue;
                            var mc = _connRegistry.Get(m.ObjectId);
                            if (mc is not null) try { await mc.SendAsync(update, ct); } catch { }
                        }
                    }
                }
            }
            var activation = new SM_SKILL_ACTIVATION(_spellId);
            await BroadcastAsync(activation, ct);
        }
        else if (isDamageSkill && _targetType is 0 or 3 or 4 && _targetObjectId != 0)
        {
            var spellId    = _spellId;
            var world      = _world;
            var registry   = _connRegistry;
            var expSvc     = _expService;
            var spawnSvc   = _spawnService;
            var lootSvc    = _lootService;
            var questSvc   = _questService;
            var duelSvc    = _duelService;
            var conn       = _conn;
            var playerDao  = _playerDao;
            var legionDao  = _legionDao;
            var rates      = _rates;

            _ = Task.Run(async () =>
            {
                if (castDelay > 0)
                    await Task.Delay(castDelay);

                int castWorldId = player.Position.WorldId;
                var activation = new SM_SKILL_ACTIVATION(spellId);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        try { await c.SendAsync(activation); } catch { }

                if (player.IsAlreadyDead) return;

                Creature? target = world.GetPlayerByObjectId(_targetObjectId)
                                ?? (Creature?)world.GetNpcByObjectId(_targetObjectId);
                if (target is null || target.IsAlreadyDead) return;
                if (target.Position.WorldId != castWorldId) return;
                if (castRange > 0f && player.Position.DistanceTo(target.Position) > castRange) return;

                int rawSpellDmg = player.Level * 8 + Random.Shared.Next(20, 60);
                bool spellIsMagical = template?.SkillType == SkillType.MAGICAL;
                int spellDef = target is Player pvpSpellTarget
                             ? (spellIsMagical ? pvpSpellTarget.MagicDefense : pvpSpellTarget.PhysicalDefense)
                             : target is Npc npcSpellTarget
                             ? (spellIsMagical ? (npcSpellTarget.Template.Stats?.MResist ?? 0)
                                               : (npcSpellTarget.Template.Stats?.PDef    ?? 0))
                             : 0;
                int damage = spellDef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + spellDef)) : rawSpellDmg;
                target.CurrentHp = Math.Max(0, target.CurrentHp - damage);

                // Both caster and target enter combat
                var combatNow = DateTime.UtcNow;
                player.LastCombatTime = combatNow;
                target.LastCombatTime = combatNow;

                if (target is Player damagedPlayer)
                {
                    var grp = damagedPlayer.Group;
                    if (grp is not null)
                    {
                        var update = new SM_GROUP_MEMBER_INFO(grp.GroupId, damagedPlayer, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
                        foreach (var m in grp.Members)
                        {
                            if (m.ObjectId == damagedPlayer.ObjectId) continue;
                            var mc = registry.Get(m.ObjectId);
                            if (mc is not null) try { await mc.SendAsync(update); } catch { }
                        }
                    }
                }

                var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, damage, SM_ATTACK_STATUS.LogId.SpellAtk);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        try { await c.SendAsync(statusPkt); } catch { }

                // DP gain on successful spell hit (150 DP per skill, capped at 6000)
                if (player.Dp < 6000)
                {
                    player.Dp = Math.Min(6000, player.Dp + 150);
                    try { await conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp)); } catch { }
                }

                if (target.CurrentHp > 0) return;

                if (target is Player deadPlayer)
                {
                    // Duel: end without killing — restore 1 HP, send result, clear duel state
                    if (duelSvc.GetOpponent(player.ObjectId) == deadPlayer.ObjectId)
                    {
                        deadPlayer.CurrentHp = 1;
                        duelSvc.EndDuel(player.ObjectId, deadPlayer.ObjectId);
                        try { await conn.SendAsync(SM_DUEL.Won(deadPlayer.Name)); } catch { }
                        var loserConn = registry.Get(deadPlayer.ObjectId);
                        if (loserConn is not null)
                            try { await loserConn.SendAsync(SM_DUEL.Lost(player.Name)); } catch { }
                        return;
                    }

                    deadPlayer.State |= CreatureState.Dead;
                    var die = new SM_EMOTION(deadPlayer, EmotionType.DIE);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(die); } catch { }
                    var targetConn = registry.Get(deadPlayer.ObjectId);
                    if (targetConn is not null) try { await targetConn.SendAsync(new SM_DIE()); } catch { }

                    // PvP AP exchange — only in Abyss/Balaurea maps AND opposing factions
                    if (AbyssRankService.IsPvPMap(castWorldId) && player.Race != deadPlayer.Race)
                    {
                        int apGain = AbyssRankService.CalculatePvPApGained(player, deadPlayer);
                        if (rates.ApPlayerGainRate != 1.0f)
                            apGain = Math.Max(1, (int)(apGain * rates.ApPlayerGainRate));
                        int apLoss = AbyssRankService.CalculatePvPApLost(player, deadPlayer);
                        bool spellPvpRankUp = AbyssRankService.AddAp(player, apGain);
                        AbyssRankService.LoseAp(deadPlayer, apLoss);
                        AbyssRankService.TrackPvPKill(player, apGain);

                        try { await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), CancellationToken.None); } catch { }
                        if (spellPvpRankUp)
                        {
                            var rankPkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(rankPkt); } catch { }
                        }
                        if (targetConn is not null)
                            try { await targetConn.SendAsync(SM_ABYSS_RANK.ForPlayer(deadPlayer), CancellationToken.None); } catch { }

                        await playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
                        await playerDao.UpdateAbyssAsync(deadPlayer.ObjectId, deadPlayer.AbyssPoints, deadPlayer.AbyssRank, CancellationToken.None);
                        await playerDao.UpdateAbyssKillStatsAsync(player.ObjectId,
                            player.AbyssAllKill, player.AbyssMaxRank,
                            player.AbyssDailyKill, player.AbyssDailyAp,
                            player.AbyssWeeklyKill, player.AbyssWeeklyAp,
                            player.AbyssLastKill, player.AbyssLastAp, CancellationToken.None);
                        await AwardLegionContributionAsync(player, apGain, registry, legionDao, CancellationToken.None);
                    }
                }
                else if (target is Npc deadNpc)
                {
                    deadNpc.State |= CreatureState.Dead;
                    int npcWorldId = deadNpc.Position.WorldId;
                    var die = new SM_EMOTION(deadNpc, EmotionType.DIE);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(die); } catch { }
                    world.Remove(deadNpc);

                    lootSvc.GenerateDrops(deadNpc);

                    // Update quest kill progress
                    await questSvc.HandleNpcKillAsync(player, deadNpc, conn, CancellationToken.None);

                    long xp = deadNpc.Template.Stats?.MaxXp > 0
                        ? deadNpc.Template.Stats.MaxXp
                        : deadNpc.Level * 50L;
                    await expSvc.AddGroupExpAsync(player, xp, CancellationToken.None);

                    // Award AP for kills in the Abyss world or against ABYSS_GUARD NPCs; persist immediately
                    if (deadNpc.Position.WorldId == AbyssRankService.AbyssWorldId
                        || deadNpc.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase))
                    {
                        int ap = AbyssRankService.CalculateNpcApReward(deadNpc.Level);
                        bool spellNpcRankUp = AbyssRankService.AddAp(player, ap);
                        if (player.AbyssRank > player.AbyssMaxRank) player.AbyssMaxRank = player.AbyssRank;
                        try { await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), CancellationToken.None); } catch { }
                        if (spellNpcRankUp)
                        {
                            var rankPkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(rankPkt); } catch { }
                        }
                        await playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
                        await AwardLegionContributionAsync(player, ap, registry, legionDao, CancellationToken.None);
                    }

                    await Task.Delay(3000);
                    var del = new SM_DELETE(deadNpc.ObjectId);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(del); } catch { }
                    spawnSvc.ScheduleRespawn(deadNpc);

                    await Task.Delay(57_000); // 60s total from kill
                    lootSvc.ClearLoot(deadNpc.ObjectId);
                }
            });
        }
        else
        {
            // Buff, chant, passive, or unknown — broadcast activation immediately
            var activation = new SM_SKILL_ACTIVATION(_spellId);
            await BroadcastAsync(activation, ct);
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

    private static async Task AwardLegionContributionAsync(Model.Player player, long apAmount,
        PlayerConnectionRegistry registry, ILegionDao legionDao, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null || apAmount <= 0) return;

        legion.ContributionPoints += apAmount;
        await legionDao.UpdateContributionPointsAsync(legion.LegionId, legion.ContributionPoints, ct);

        var update = new SM_LEGION_EDIT(legion.ContributionPoints);
        foreach (var member in legion.Members.Values)
        {
            var mConn = registry.Get(member.ObjectId);
            if (mConn is not null)
                try { await mConn.SendAsync(update, ct); } catch { }
        }
    }
}
