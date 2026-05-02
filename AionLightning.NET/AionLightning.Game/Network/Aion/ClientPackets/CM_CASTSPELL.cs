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
    private readonly NpcAiService _npcAi;
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
        QuestService questService, DuelService duelService, NpcAiService npcAi,
        IPlayerDao playerDao, ILegionDao legionDao, RateOptions rates)
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
        _npcAi        = npcAi;
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

        var template = _dataManager.Skills.GetTemplate(_spellId);

        // Server-side cooldown enforcement keyed by CooldownId group (mirrors Java isSkillDisabled)
        if (template is not null && template.Cooldown > 0)
        {
            int cdId = template.EffectiveCooldownId;
            if (player.IsSkillOnCooldown(cdId)) return;
            player.SetSkillCooldown(cdId, template.Cooldown);
            await _conn.SendAsync(new SM_SKILL_COOLDOWN(_dataManager.Skills, player.SkillCooldowns), ct);
        }

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
                // Java AbstractHealEffect: healBoost adds additively (1000 = +100%); simplified to multiplicative factor
                int heal = (int)((player.Level * 6 + Random.Shared.Next(15, 40)) * (1.0f + player.BonusHealBoost / 1000f));
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
        else if (template?.SubType is SkillSubType.BUFF or SkillSubType.CHANT
                 && _targetType is 0 or 3 or 4 && template.Duration > 0)
        {
            // Determine buff target: self when targetObjectId is 0 or caster's own id
            Creature? buffTarget = (_targetObjectId == 0 || _targetObjectId == player.ObjectId)
                ? player
                : _world.GetPlayerByObjectId(_targetObjectId);

            if (buffTarget is not null && !buffTarget.IsAlreadyDead)
            {
                int durationMs = template.Duration;
                var effect = new AbnormalState
                {
                    SkillId    = _spellId,
                    SkillLevel = _level,
                    EffectorId = player.ObjectId,
                    Expiry     = DateTime.UtcNow.AddMilliseconds(durationMs),
                };
                buffTarget.AddEffect(effect);

                bool buffTargetIsPlayer = buffTarget is Player;
                int  buffWorldId        = player.Position.WorldId;
                var  abnormal = new SM_ABNORMAL_EFFECT(buffTarget.ObjectId, buffTargetIsPlayer,
                                    buffTarget.GetActiveEffects());
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == buffWorldId)
                        try { await c.SendAsync(abnormal, ct); } catch { }

                // Schedule expiry — remove effect and re-broadcast to target's current zone
                var expiryEffect = effect;
                var expiryTarget = buffTarget;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(durationMs);
                    expiryTarget.RemoveEffect(expiryEffect.SkillId, expiryEffect.Expiry);
                    var expired = new SM_ABNORMAL_EFFECT(expiryTarget.ObjectId, buffTargetIsPlayer,
                                      expiryTarget.GetActiveEffects());
                    int expWorldId = expiryTarget.Position.WorldId;
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == expWorldId)
                            try { await c.SendAsync(expired); } catch { }
                });
            }

            await BroadcastAsync(new SM_SKILL_ACTIVATION(_spellId), ct);
        }
        else if (isDamageSkill && _targetType is 1 or 2 && template?.IsGroundAoe == true)
        {
            // Ground-targeted AoE (first_target=POINT, target_type=AREA): damage all enemies near the cast point
            var spellId  = _spellId;
            var world    = _world;
            var registry = _connRegistry;
            var conn     = _conn;
            float px = _x, py = _y, pz = _z;
            float aoeR    = Math.Max(1f, template.EffectiveRange);
            float aoeAlt  = Math.Max(1f, template.EffectiveAltitude);
            int   maxHits = template.TargetMaxCount;
            bool  hitsEnemies = !string.Equals(template.TargetRelation, "FRIEND", StringComparison.OrdinalIgnoreCase);

            _ = Task.Run(async () =>
            {
                if (castDelay > 0)
                    await Task.Delay(castDelay);

                if (player.IsAlreadyDead) return;

                int castWorldId = player.Position.WorldId;
                var activation  = new SM_SKILL_ACTIVATION(spellId);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        try { await c.SendAsync(activation); } catch { }

                // Collect targets within the AoE cylinder (horizontal dist + altitude filter)
                var targets = new List<Creature>();
                if (hitsEnemies)
                {
                    foreach (var npc in world.GetAllNpcs())
                    {
                        if (npc.IsAlreadyDead) continue;
                        if (npc.Position.WorldId != castWorldId) continue;
                        float dx = npc.Position.X - px, dy = npc.Position.Y - py, dz = npc.Position.Z - pz;
                        if (dx * dx + dy * dy > aoeR * aoeR) continue;
                        if (Math.Abs(dz) > aoeAlt) continue;
                        targets.Add(npc);
                        if (targets.Count >= maxHits) break;
                    }
                    // Also hit players from opposing faction (PvP zones) within AoE
                    foreach (var other in world.GetAll())
                    {
                        if (other.IsAlreadyDead || other.ObjectId == player.ObjectId) continue;
                        if (other.Race == player.Race) continue;
                        if (other.Position.WorldId != castWorldId) continue;
                        float dx = other.Position.X - px, dy = other.Position.Y - py, dz = other.Position.Z - pz;
                        if (dx * dx + dy * dy > aoeR * aoeR) continue;
                        if (Math.Abs(dz) > aoeAlt) continue;
                        targets.Add(other);
                        if (targets.Count >= maxHits) break;
                    }
                }
                else
                {
                    // FRIEND relation: heal/buff allies in area (resolve as heal at group-heal formula)
                    foreach (var ally in world.GetAll())
                    {
                        if (ally.IsAlreadyDead) continue;
                        if (ally.Race != player.Race && ally.ObjectId != player.ObjectId) continue;
                        if (ally.Position.WorldId != castWorldId) continue;
                        float dx = ally.Position.X - px, dy = ally.Position.Y - py, dz = ally.Position.Z - pz;
                        if (dx * dx + dy * dy > aoeR * aoeR) continue;
                        if (Math.Abs(dz) > aoeAlt) continue;
                        targets.Add(ally);
                        if (targets.Count >= maxHits) break;
                    }
                }

                bool spellIsMagical = template?.SkillType == SkillType.MAGICAL;
                int mAtk = 100 + player.MainHandMagicalAtk + player.BonusMagicAtk;
                int pAtk = player.BasePhysicalAttack + (player.MainHandMinDmg + player.MainHandMaxDmg) / 2 + player.BonusPhysicalAtk;

                foreach (var target in targets)
                {
                    // Java: magicBoost -= getMBResist() (target suppression reduces caster boost, min 0)
                    int tMBSuppress = spellIsMagical
                        ? (target is Player pvpSupp ? pvpSupp.BonusMagicSuppression
                         : target is Npc npcSupp ? (npcSupp.Template.Stats?.MBResist ?? 0)
                         : 0)
                        : 0;
                    float magicBoostMult = 1.0f + Math.Max(0, player.BonusMagicBoost - tMBSuppress) / 1000f;

                    int rawSpellDmg = spellIsMagical
                        ? (int)((mAtk + player.Level * 6 + Random.Shared.Next(10, 40)) * magicBoostMult)
                        : pAtk + player.Level * 4 + Random.Shared.Next(10, 40);

                    // Magic resist check (Java calculateMagicalResistRate)
                    if (spellIsMagical)
                    {
                        int totalMagicAcc = player.BaseMagicAccuracy + player.BonusMagicalAccuracy;
                        int targetMR = target is Player pvpResist ? pvpResist.BonusMagicResist
                                     : target is Npc npcResist   ? NpcMagicResist(npcResist)
                                     : 0;
                        int resistRate = Math.Max(1, targetMR - totalMagicAcc);
                        int tLvlAoE = target is Player pvpLvl ? pvpLvl.Level : target is Npc npcLvl ? npcLvl.Level : 0;
                        int lvlDiffAoE = tLvlAoE - player.Level - 2;
                        if (lvlDiffAoE > 0) resistRate += lvlDiffAoE * 100;
                        if (Random.Shared.Next(1000) < resistRate)
                        {
                            var resistPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(resistPkt); } catch { }
                            continue;
                        }
                    }

                    // Magical crit check (same piecewise formula as physical crit)
                    if (spellIsMagical)
                    {
                        int mCritRating = player.BaseMagicCritRating + player.BonusMagicalCritical;
                        int mCritResist = target is Player pvpMCrit ? pvpMCrit.BonusMagicalCriticalResist : 0;
                        mCritRating = Math.Max(0, mCritRating - mCritResist);
                        double mCritRate = mCritRating <= 440 ? mCritRating * 0.1
                                         : mCritRating <= 600 ? 44.0 + (mCritRating - 440) * 0.05
                                         : 52.0 + (mCritRating - 600) * 0.02;
                        if (Random.Shared.Next(100) < (int)mCritRate)
                        {
                            int spF = target is Player pvpSpF ? pvpSpF.BonusSpellFortitude : 0;
                            float mCritCoeff = Math.Max(1.0f, 1.5f - (float)Math.Round(spF / 1000.0));
                            rawSpellDmg = (int)(rawSpellDmg * mCritCoeff);
                        }
                    }

                    // NPC level-diff damage reduction (Java StatFunctions.adjustDamages, applies to all damage types)
                    if (target is Npc npcLvlAoEDmg)
                    {
                        float lvlMod = NpcLevelDiffMod(npcLvlAoEDmg.Level - player.Level);
                        if (lvlMod > 0f) rawSpellDmg = Math.Max(1, (int)(rawSpellDmg * (1f - lvlMod)));
                    }

                    int spellDef = target is Player pvpSpellTarget
                                 ? (spellIsMagical ? pvpSpellTarget.MagicDefense : pvpSpellTarget.PhysicalDefense)
                                 : target is Npc npcSpellTarget
                                 ? (spellIsMagical ? NpcMagicResist(npcSpellTarget)
                                                   : (npcSpellTarget.Template.Stats?.PDef    ?? 0))
                                 : 0;
                    int damage = spellDef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + spellDef)) : rawSpellDmg;
                    target.CurrentHp      = Math.Max(0, target.CurrentHp - damage);
                    target.LastCombatTime = DateTime.UtcNow;
                    player.LastCombatTime = DateTime.UtcNow;

                    if (target is Npc hitNpc && target.CurrentHp > 0)
                        _npcAi.ForceEngage(hitNpc, player);

                    var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, damage, SM_ATTACK_STATUS.LogId.SpellAtk);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(statusPkt); } catch { }
                }

                // DP gain for successful AoE cast
                if (targets.Count > 0 && player.Dp < 6000)
                {
                    player.Dp = Math.Min(6000, player.Dp + 150);
                    try { await conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp)); } catch { }
                }

                // Handle killed NPCs: broadcast death, generate drops, award XP/AP
                foreach (var killed in targets.OfType<Npc>().Where(t => t.CurrentHp <= 0))
                {
                    killed.State |= CreatureState.Dead;
                    int npcWorldId = killed.Position.WorldId;
                    var die = new SM_EMOTION(killed, EmotionType.DIE);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(die); } catch { }

                    var diedShout = _dataManager.NpcShouts.GetRandomShout(
                        killed.Template.NpcId, NpcShoutData.ShoutEventType.DIED, npcWorldId);
                    if (diedShout.HasValue)
                    {
                        var shoutPkt = SM_SYSTEM_MESSAGE.NpcShout(killed.ObjectId, diedShout.Value.StringId);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                                try { await c.SendAsync(shoutPkt); } catch { }
                    }

                    world.Remove(killed);
                    _lootService.GenerateDrops(killed, player);
                    await _questService.HandleNpcKillAsync(player, killed, conn, CancellationToken.None);

                    long xpBase = killed.Template.Stats?.MaxXp > 0 ? killed.Template.Stats.MaxXp : killed.Level * 50L;
                    await _expService.AddGroupExpAsync(player, xpBase, killed.Level, CancellationToken.None);

                    if (killed.Position.WorldId == AbyssRankService.AbyssWorldId
                        || killed.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase))
                    {
                        int ap = AbyssRankService.CalculateNpcApReward(killed.Level);
                        bool aoeNpcRankUp = AbyssRankService.AddAp(player, ap);
                        if (player.AbyssRank > player.AbyssMaxRank) player.AbyssMaxRank = player.AbyssRank;
                        try { await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), CancellationToken.None); } catch { }
                        if (aoeNpcRankUp)
                        {
                            var rankPkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(rankPkt); } catch { }
                        }
                        await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
                        await AwardLegionContributionAsync(player, ap, registry, _legionDao, CancellationToken.None);
                    }

                    var killedNpc    = killed;
                    var killedDrops  = _lootService.GetLoot(killedNpc.ObjectId);
                    int killedDecayMs = killedDrops is null ? 5_000 : killedDrops.Count == 0 ? 90_000 : 300_000;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(killedDecayMs);
                        var del = new SM_DELETE(killedNpc.ObjectId);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == killedNpc.Position.WorldId)
                                try { await c.SendAsync(del); } catch { }
                        _lootService.ClearLoot(killedNpc.ObjectId);
                        _spawnService.ScheduleRespawn(killedNpc);
                    });
                }
            });
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
            var npcAi      = _npcAi;
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

                bool spellIsMagical = template?.SkillType == SkillType.MAGICAL;

                // Magic resist check (Java calculateMagicalResistRate):
                // resistRate = max(1, target.MagicResist - player.MagicAccuracy) + level-diff bonus; out of 1000
                if (spellIsMagical)
                {
                    int totalMagicAcc = player.BaseMagicAccuracy + player.BonusMagicalAccuracy;
                    int targetMagicResist = target is Player pvpResistTarget ? pvpResistTarget.BonusMagicResist
                                         : target is Npc npcResistTarget    ? NpcMagicResist(npcResistTarget)
                                         : 0;
                    int resistRate = Math.Max(1, targetMagicResist - totalMagicAcc);
                    int tLvlST = target is Player pvpSTLvl ? pvpSTLvl.Level : target is Npc npcSTLvl ? npcSTLvl.Level : 0;
                    int lvlDiffST = tLvlST - player.Level - 2;
                    if (lvlDiffST > 0) resistRate += lvlDiffST * 100;
                    if (Random.Shared.Next(1000) < resistRate)
                    {
                        // Spell resisted — send 0-damage status and return
                        var resistPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(resistPkt); } catch { }
                        return;
                    }
                }

                int rawSpellDmg;
                if (spellIsMagical)
                {
                    int mAtkG = 100 + player.MainHandMagicalAtk + player.BonusMagicAtk;
                    int tMBSuppressG = target is Player pvpSuppG ? pvpSuppG.BonusMagicSuppression
                                     : target is Npc npcSuppG ? (npcSuppG.Template.Stats?.MBResist ?? 0) : 0;
                    float mbMultG = 1.0f + Math.Max(0, player.BonusMagicBoost - tMBSuppressG) / 1000f;
                    rawSpellDmg = (int)((mAtkG + player.Level * 6 + Random.Shared.Next(10, 40)) * mbMultG);
                }
                else
                {
                    int pAtkG = player.BasePhysicalAttack + (player.MainHandMinDmg + player.MainHandMaxDmg) / 2 + player.BonusPhysicalAtk;
                    rawSpellDmg = pAtkG + player.Level * 4 + Random.Shared.Next(10, 40);
                }

                // Magical crit check (Java calculateMagicalCriticalRate, same piecewise formula as physical)
                if (spellIsMagical)
                {
                    int mCritRating = player.BaseMagicCritRating + player.BonusMagicalCritical;
                    int mCritResist = target is Player pvpMCritTarget ? pvpMCritTarget.BonusMagicalCriticalResist : 0;
                    mCritRating = Math.Max(0, mCritRating - mCritResist);
                    double mCritRate = mCritRating <= 440 ? mCritRating * 0.1
                                     : mCritRating <= 600 ? 44.0 + (mCritRating - 440) * 0.05
                                     : 52.0 + (mCritRating - 600) * 0.02;
                    if (Random.Shared.Next(100) < (int)mCritRate)
                    {
                        int spFt = target is Player pvpSpFt ? pvpSpFt.BonusSpellFortitude : 0;
                        float mCritCoeffG = Math.Max(1.0f, 1.5f - (float)Math.Round(spFt / 1000.0));
                        rawSpellDmg = (int)(rawSpellDmg * mCritCoeffG);
                    }
                }

                // NPC level-diff damage reduction (Java StatFunctions.adjustDamages)
                if (target is Npc npcLvlSTDmg)
                {
                    float lvlModST = NpcLevelDiffMod(npcLvlSTDmg.Level - player.Level);
                    if (lvlModST > 0f) rawSpellDmg = Math.Max(1, (int)(rawSpellDmg * (1f - lvlModST)));
                }

                int spellDef = target is Player pvpSpellTarget
                             ? (spellIsMagical ? pvpSpellTarget.MagicDefense : pvpSpellTarget.PhysicalDefense)
                             : target is Npc npcSpellTarget
                             ? (spellIsMagical ? NpcMagicResist(npcSpellTarget)
                                               : (npcSpellTarget.Template.Stats?.PDef    ?? 0))
                             : 0;
                int damage = spellDef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + spellDef)) : rawSpellDmg;
                target.CurrentHp = Math.Max(0, target.CurrentHp - damage);

                // Both caster and target enter combat
                var combatNow = DateTime.UtcNow;
                player.LastCombatTime = combatNow;
                target.LastCombatTime = combatNow;

                // NPC retaliation: force NPC to engage the caster when hit by a spell
                if (target is Npc spellHitNpc && target.CurrentHp > 0)
                    npcAi.ForceEngage(spellHitNpc, player);

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

                // Caster/target-centered AoE splash: damage all additional enemies within effective_range
                if ((template?.IsCasterAoe == true || template?.IsTargetAoe == true) && template.EffectiveRange > 0)
                {
                    float aoeR   = template.EffectiveRange;
                    float aoeAlt = Math.Max(1f, template.EffectiveAltitude);
                    int   maxHits = template.TargetMaxCount;
                    Position center = template.IsCasterAoe ? player.Position : target.Position;
                    bool hitsEnemies = !string.Equals(template.TargetRelation, "FRIEND",
                                           StringComparison.OrdinalIgnoreCase);
                    int splashCount = 1; // primary target already counted

                    var splashNpcs = world.GetAllNpcs()
                        .Where(n => !n.IsAlreadyDead
                                 && n.ObjectId != target.ObjectId
                                 && n.Position.WorldId == castWorldId
                                 && hitsEnemies)
                        .Where(n => {
                            float dx = n.Position.X - center.X, dy = n.Position.Y - center.Y, dz = n.Position.Z - center.Z;
                            return dx * dx + dy * dy <= aoeR * aoeR && Math.Abs(dz) <= aoeAlt;
                        });

                    foreach (var splash in splashNpcs)
                    {
                        if (splashCount >= maxHits) break;
                        splashCount++;

                        int splashMBSuppress = spellIsMagical ? (splash.Template.Stats?.MBResist ?? 0) : 0;
                        float splashMBMult = 1.0f + Math.Max(0, player.BonusMagicBoost - splashMBSuppress) / 1000f;
                        int splashRaw = spellIsMagical
                            ? (int)(((100 + player.MainHandMagicalAtk + player.BonusMagicAtk) + player.Level * 6 + Random.Shared.Next(10, 40)) * splashMBMult)
                            : (player.BasePhysicalAttack + (player.MainHandMinDmg + player.MainHandMaxDmg) / 2 + player.BonusPhysicalAtk) + player.Level * 4 + Random.Shared.Next(10, 40);

                        // Magic resist check for AoE splash
                        if (spellIsMagical)
                        {
                            int totalMagicAccS = player.BaseMagicAccuracy + player.BonusMagicalAccuracy;
                            int splashMR = NpcMagicResist(splash);
                            int splashResistRate = Math.Max(1, splashMR - totalMagicAccS);
                            int splashLvlDiff = splash.Level - player.Level - 2;
                            if (splashLvlDiff > 0) splashResistRate += splashLvlDiff * 100;
                            if (Random.Shared.Next(1000) < splashResistRate)
                            {
                                var resistPktS = new SM_ATTACK_STATUS(splash, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                        try { await c.SendAsync(resistPktS); } catch { }
                                continue;
                            }
                        }

                        // Magical crit check for AoE splash
                        if (spellIsMagical)
                        {
                            int mCritRatingS = player.BaseMagicCritRating + player.BonusMagicalCritical;
                            double mCritRateS = mCritRatingS <= 440 ? mCritRatingS * 0.1
                                              : mCritRatingS <= 600 ? 44.0 + (mCritRatingS - 440) * 0.05
                                              : 52.0 + (mCritRatingS - 600) * 0.02;
                            if (Random.Shared.Next(100) < (int)mCritRateS)
                                splashRaw = (int)(splashRaw * 1.5f);
                        }

                        int splashDef = spellIsMagical ? NpcMagicResist(splash)
                                                       : (splash.Template.Stats?.PDef    ?? 0);
                        int splashDmg = splashDef > 0 ? Math.Max(1, splashRaw * 1000 / (1000 + splashDef)) : splashRaw;
                        splash.CurrentHp      = Math.Max(0, splash.CurrentHp - splashDmg);
                        splash.LastCombatTime = DateTime.UtcNow;

                        if (splash.CurrentHp > 0)
                            npcAi.ForceEngage(splash, player);

                        var splashPkt = new SM_ATTACK_STATUS(splash, SM_ATTACK_STATUS.AttackType.Damage, spellId, splashDmg, SM_ATTACK_STATUS.LogId.SpellAtk);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(splashPkt); } catch { }

                        if (splash.CurrentHp <= 0)
                        {
                            splash.State |= CreatureState.Dead;
                            int splashWorld = splash.Position.WorldId;
                            var splashDie = new SM_EMOTION(splash, EmotionType.DIE);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == splashWorld)
                                    try { await c.SendAsync(splashDie); } catch { }

                            var shout = _dataManager.NpcShouts.GetRandomShout(
                                splash.Template.NpcId, NpcShoutData.ShoutEventType.DIED, splashWorld);
                            if (shout.HasValue)
                            {
                                var shoutPkt2 = SM_SYSTEM_MESSAGE.NpcShout(splash.ObjectId, shout.Value.StringId);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == splashWorld)
                                        try { await c.SendAsync(shoutPkt2); } catch { }
                            }

                            world.Remove(splash);
                            _lootService.GenerateDrops(splash, player);
                            await _questService.HandleNpcKillAsync(player, splash, conn, CancellationToken.None);
                            long splashXp = splash.Template.Stats?.MaxXp > 0 ? splash.Template.Stats.MaxXp : splash.Level * 50L;
                            await _expService.AddGroupExpAsync(player, splashXp, splash.Level, CancellationToken.None);

                            var deadSplash    = splash;
                            var splashDrops   = _lootService.GetLoot(deadSplash.ObjectId);
                            int splashDecayMs = splashDrops is null ? 5_000 : splashDrops.Count == 0 ? 90_000 : 300_000;
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(splashDecayMs);
                                var del = new SM_DELETE(deadSplash.ObjectId);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == deadSplash.Position.WorldId)
                                        try { await c.SendAsync(del); } catch { }
                                _lootService.ClearLoot(deadSplash.ObjectId);
                                _spawnService.ScheduleRespawn(deadSplash);
                            });
                        }
                    }
                }

                // Apply DEBUFF visual effect when skill has DEBUFF subtype and a duration
                if (target.CurrentHp > 0
                    && template?.SubType == SkillSubType.DEBUFF && template.Duration > 0)
                {
                    bool debuffTargetIsPlayer = target is Player;
                    var  debuffEffect = new AbnormalState
                    {
                        SkillId    = spellId,
                        SkillLevel = _level,
                        EffectorId = player.ObjectId,
                        Expiry     = DateTime.UtcNow.AddMilliseconds(template.Duration),
                    };
                    target.AddEffect(debuffEffect);
                    var debuffAbnormal = new SM_ABNORMAL_EFFECT(target.ObjectId, debuffTargetIsPlayer,
                                            target.GetActiveEffects());
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(debuffAbnormal); } catch { }

                    // Schedule expiry broadcast at target's current zone
                    var expEffect = debuffEffect;
                    var expTarget = target;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(template.Duration);
                        expTarget.RemoveEffect(expEffect.SkillId, expEffect.Expiry);
                        var expired = new SM_ABNORMAL_EFFECT(expTarget.ObjectId, debuffTargetIsPlayer,
                                          expTarget.GetActiveEffects());
                        int expWorldId = expTarget.Position.WorldId;
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == expWorldId)
                                try { await c.SendAsync(expired); } catch { }
                    });
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
                    deadPlayer.ClearAllEffects();
                    var die         = new SM_EMOTION(deadPlayer, EmotionType.DIE);
                    var clearEffect = new SM_ABNORMAL_EFFECT(deadPlayer.ObjectId, isPlayer: true);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        {
                            try { await c.SendAsync(die); } catch { }
                            try { await c.SendAsync(clearEffect); } catch { }
                        }
                    var targetConn = registry.Get(deadPlayer.ObjectId);
                    if (targetConn is not null)
                    {
                        try { await targetConn.SendAsync(new SM_DIE()); } catch { }
                        try { await targetConn.SendAsync(SM_SYSTEM_MESSAGE.YouWereKilledBy(player.Name)); } catch { }
                    }

                    // Zone-wide kill announcement: "%0 was killed by %1's attack."
                    var killAnnounce = SM_SYSTEM_MESSAGE.PlayerKilledByPlayer(deadPlayer.Name, player.Name);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(killAnnounce); } catch { }

                    // Group members see "[player] has died."
                    var deadGroup = deadPlayer.Group;
                    if (deadGroup is not null)
                    {
                        var groupDied = SM_SYSTEM_MESSAGE.GroupMemberDied(deadPlayer.Name);
                        foreach (var m in deadGroup.Members)
                        {
                            if (m.ObjectId == deadPlayer.ObjectId) continue;
                            var mc = registry.Get(m.ObjectId);
                            if (mc is not null) try { await mc.SendAsync(groupDied); } catch { }
                        }
                    }

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

                    // DIED shout — NPC death cry
                    var diedShout = _dataManager.NpcShouts.GetRandomShout(
                        deadNpc.Template.NpcId, NpcShoutData.ShoutEventType.DIED, npcWorldId);
                    if (diedShout.HasValue)
                    {
                        var shoutPkt = SM_SYSTEM_MESSAGE.NpcShout(deadNpc.ObjectId, diedShout.Value.StringId);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                                try { await c.SendAsync(shoutPkt); } catch { }
                    }

                    world.Remove(deadNpc);

                    lootSvc.GenerateDrops(deadNpc, player);

                    // Update quest kill progress
                    await questSvc.HandleNpcKillAsync(player, deadNpc, conn, CancellationToken.None);

                    // Award XP — level-diff scaling and group distribution handled inside AddGroupExpAsync
                    long xpBase = deadNpc.Template.Stats?.MaxXp > 0
                        ? deadNpc.Template.Stats.MaxXp
                        : deadNpc.Level * 50L;
                    await expSvc.AddGroupExpAsync(player, xpBase, deadNpc.Level, CancellationToken.None);

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

                    var stDrops  = lootSvc.GetLoot(deadNpc.ObjectId);
                    int stDecayMs = stDrops is null ? 5_000 : stDrops.Count == 0 ? 90_000 : 300_000;
                    await Task.Delay(stDecayMs);
                    var del = new SM_DELETE(deadNpc.ObjectId);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(del); } catch { }
                    lootSvc.ClearLoot(deadNpc.ObjectId);
                    spawnSvc.ScheduleRespawn(deadNpc);
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

    // Java NpcGameStats.getMResist(): base = round(level*17.5+75) when mRes==0; template MResist adds on top
    private static int NpcMagicResist(Model.Npc npc)
    {
        int @base = (int)Math.Round(npc.Level * 17.5f + 75);
        int tpl   = npc.Template.Stats?.MResist ?? 0;
        return tpl > 0 ? tpl : @base;
    }

    // Java StatFunctions.getNpcLevelDiffMod: multiplier for dodge and damage when NPC > player level
    private static float NpcLevelDiffMod(int levelDiff) => levelDiff switch
    {
        3 => 0.1f, 4 => 0.2f, 5 => 0.3f, 6 => 0.4f,
        7 => 0.5f, 8 => 0.6f, 9 => 0.7f,
        _ => levelDiff > 9 ? 0.8f : 0f
    };

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
