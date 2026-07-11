using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Combat;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Ai;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_ATTACK : AionClientPacket
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
    private readonly IEventBus _eventBus;

    private int _targetObjectId;
    private int _time;

    public CM_ATTACK(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ExperienceService expService,
        SpawnService spawnService, LootService lootService, QuestService questService,
        DuelService duelService, NpcAiService npcAi,
        IPlayerDao playerDao, ILegionDao legionDao, IEventBus eventBus)
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
        _eventBus     = eventBus;
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

        // Java Creature.canAttack(): block if CANT_ATTACK_STATE (stun/sleep/paralyze/etc.) is active
        if ((player.ActiveCcFlags & AbnormalCcFlags.CantAttack) != 0) return;

        // M292: blind — forced DODGE when blinded (Java AttackCalcObserver: Rnd.get(0,100) <= value → DODGE)
        if ((player.ActiveCcFlags & AbnormalCcFlags.Blind) != 0)
        {
            var blindEffect = player.GetActiveEffects().FirstOrDefault(e =>
                (e.CcFlags & AbnormalCcFlags.Blind) != 0 && !e.IsExpired);
            if (blindEffect is not null && Random.Shared.Next(101) <= blindEffect.BlindDodgePct)
            {
                // Resolve target for the dodge packet (silently skip if no longer present)
                Creature? blindTarget = _world.GetPlayerByObjectId(_targetObjectId)
                                     ?? (Creature?)_world.GetNpcByObjectId(_targetObjectId);
                if (blindTarget is not null)
                    await BroadcastAsync(new SM_ATTACK(player, blindTarget, attackno: 0, time: (short)_time, type: 0, 0, SM_ATTACK.HitResult.Dodge), ct);
                return;
            }
        }

        // M301: reveal player when auto-attacking while hidden
        if (player.IsHidden)
        {
            player.IsHidden = false;
            var revealPkt = new SM_PLAYER_STATE(player.ObjectId, visualState: 0);
            int revWorld = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == revWorld)
                    try { await c.SendAsync(revealPkt, ct); } catch { }
        }

        // Enforce auto-attack cooldown (weapon attack speed; default 1500ms)
        var now = DateTime.UtcNow;
        int effectiveAtkSpd = Math.Max(500, player.CurrentAttackSpeed + player.AtkSpeedDebuffDelta + player.AtkSpeedStatUpDelta);
        if ((now - player.LastAttackTime).TotalMilliseconds < effectiveAtkSpd) return;
        player.LastAttackTime = now;

        // Resolve target from world
        Creature? target = _world.GetPlayerByObjectId(_targetObjectId)
                        ?? (Creature?)_world.GetNpcByObjectId(_targetObjectId);
        if (target is null || target.IsAlreadyDead) return;

        // Target must be in same zone and within melee range
        if (target.Position.WorldId != player.Position.WorldId) return;
        if (player.Position.DistanceTo(target.Position) > MaxMeleeRange) return;

        // Hit/miss check — Java calculatePhysicalDodgeRate: dodge = evasion-accuracy, NPC gets level-diff multiplier
        int totalAccuracy = player.BasePhysicalAccuracy + player.BonusPhysicalAccuracy + player.PhysAccDelta;
        int targetEvasion = (target is Player pvpEvade  ? pvpEvade.BaseEvasion + pvpEvade.BonusEvasion
                          : target is Npc npcEvade     ? NpcPhysicalAccuracy(npcEvade) + (npcEvade.Template.Stats?.Evasion ?? 0)
                          : 0) + target.EvasionDebuffDelta + target.EvasionStatUpDelta;
        float rawDodgeDiff = targetEvasion - totalAccuracy;
        if (target is Npc npcDodge)
            rawDodgeDiff *= 1f + NpcLevelDiffMod(npcDodge.Level - player.Level);
        float dodgeRate = Math.Clamp(rawDodgeDiff * 0.6f + 50f, 0f, 300f);
        if (Random.Shared.Next(1000) < (int)dodgeRate)
        {
            await BroadcastAsync(new SM_ATTACK(player, target, attackno: 0, time: (short)_time, type: 0, 0, SM_ATTACK.HitResult.Dodge), ct);
            return;
        }

        // Parry check — Java calculatePhysicalParryRate: diff=(parry-accuracy)*0.6+50, max 400/1000 (players only; NPCs have no parry stat)
        if (target is Player pvpParry)
        {
            int totalParry = pvpParry.BaseParry + pvpParry.BonusParry + pvpParry.ParryDelta;
            if (totalParry > 0)
            {
                float parryRate = Math.Clamp((totalParry - totalAccuracy) * 0.6f + 50f, 0f, 400f);
                if (Random.Shared.Next(1000) < (int)parryRate)
                {
                    // Parry: 40% reduction; PvP 50% reduction applied first (Java adjustDamages)
                    int baseAtkPr = (player.BasePhysicalAttack > 0 ? player.BasePhysicalAttack : player.Level * 6) + player.BonusPhysicalAtk + player.PatkDebuffDelta + player.PatkStatUpDelta;
                    int rawDmgPr  = player.MainHandMinDmg > 0
                        ? Random.Shared.Next(player.MainHandMinDmg, Math.Max(player.MainHandMinDmg + 1, player.MainHandMaxDmg + 1)) + baseAtkPr
                        : baseAtkPr + Random.Shared.Next(10, 40);
                    rawDmgPr     = Math.Max(1, rawDmgPr / 2); // PvP 50%
                    int pdefPr    = Math.Max(0, pvpParry.PhysicalDefense + pvpParry.PdefDebuffDelta + pvpParry.PdefStatUpDelta);
                    int dmgPr     = pdefPr > 0 ? Math.Max(1, rawDmgPr * 1000 / (1000 + pdefPr)) : rawDmgPr;
                    int parryDmg  = (int)(dmgPr * 0.6f);
                    bool parrySuppress = target is Player parryPvp && _duelService.GetOpponent(player.ObjectId) == parryPvp.ObjectId;
                    await target.ApplyDamageAndPublishAsync(player, parryDmg, DamageKind.AutoAttack, skillId: null, _eventBus, ct, suppressDeathEvent: parrySuppress);
                    await BroadcastAsync(new SM_ATTACK(player, target, attackno: 0, time: (short)_time, type: 0, parryDmg, SM_ATTACK.HitResult.Parry), ct);
                    await BroadcastAsync(new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, parryDmg), ct);
                    goto afterAttack;
                }
            }
        }

        // Block check — Java calculatePhysicalBlockRate: diff=(block-accuracy), max 500/1000 (players only)
        if (target is Player pvpBlock)
        {
            int totalBlock = pvpBlock.BaseBlock + pvpBlock.BonusBlock + pvpBlock.BlockDelta + pvpBlock.PassiveBonusBlock;
            if (totalBlock > 0)
            {
                float blockRate = Math.Clamp(totalBlock - totalAccuracy, 0f, 500f);
                if (Random.Shared.Next(1000) < (int)blockRate)
                {
                    // Block: 50% reduction; PvP 50% reduction applied first (Java adjustDamages)
                    int baseAtkBl = (player.BasePhysicalAttack > 0 ? player.BasePhysicalAttack : player.Level * 6) + player.BonusPhysicalAtk + player.PatkDebuffDelta + player.PatkStatUpDelta;
                    int rawDmgBl  = player.MainHandMinDmg > 0
                        ? Random.Shared.Next(player.MainHandMinDmg, Math.Max(player.MainHandMinDmg + 1, player.MainHandMaxDmg + 1)) + baseAtkBl
                        : baseAtkBl + Random.Shared.Next(10, 40);
                    rawDmgBl     = Math.Max(1, rawDmgBl / 2); // PvP 50%
                    int pdefBl    = Math.Max(0, pvpBlock.PhysicalDefense + pvpBlock.PdefDebuffDelta + pvpBlock.PdefStatUpDelta);
                    int dmgBl     = pdefBl > 0 ? Math.Max(1, rawDmgBl * 1000 / (1000 + pdefBl)) : rawDmgBl;
                    int blockDmg  = dmgBl / 2;
                    bool blockSuppress = target is Player blockPvp && _duelService.GetOpponent(player.ObjectId) == blockPvp.ObjectId;
                    await target.ApplyDamageAndPublishAsync(player, blockDmg, DamageKind.AutoAttack, skillId: null, _eventBus, ct, suppressDeathEvent: blockSuppress);
                    await BroadcastAsync(new SM_ATTACK(player, target, attackno: 0, time: (short)_time, type: 0, blockDmg, SM_ATTACK.HitResult.Block), ct);
                    await BroadcastAsync(new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, blockDmg), ct);
                    goto afterAttack;
                }
            }
        }

        // Physical damage: weapon + base stat + accessory P-attack bonus, or stat-based fallback
        int baseAtk = (player.BasePhysicalAttack > 0 ? player.BasePhysicalAttack : player.Level * 6) + player.BonusPhysicalAtk + player.PatkDebuffDelta + player.PatkStatUpDelta;
        int rawDmg = player.MainHandMinDmg > 0
            ? Random.Shared.Next(player.MainHandMinDmg, Math.Max(player.MainHandMinDmg + 1, player.MainHandMaxDmg + 1)) + baseAtk
            : baseAtk + Random.Shared.Next(10, 40);

        // Critical hit — Java calculatePhysicalCriticalRate piecewise: <=440: rate*0.1, <=600: 44+(r-440)*0.05, else +0.02
        int critRating = player.BaseCritRating + player.BonusPhysicalCritical + player.PhysCritDelta;
        int critResist = target is Player pvpCritTarget ? pvpCritTarget.BonusPhysicalCriticalResist + pvpCritTarget.PhysCritResistDelta : 0;
        critRating = Math.Max(0, critRating - critResist);
        double critRate = critRating <= 440 ? critRating * 0.1
                        : critRating <= 600 ? 44.0 + (critRating - 440) * 0.05
                        : 52.0 + (critRating - 600) * 0.02;
        bool isCrit = Random.Shared.Next(100) < (int)critRate;
        if (isCrit)
        {
            // Java calculateWeaponCritical: coefficient by weapon type, reduced by strikeFortitude/1000, min 1.0
            int sFortitude = target is Player pvpSF ? pvpSF.BonusStrikeFortitude + pvpSF.StrikeFortitudeDelta : 0;
            float baseCoeff = player.MainHandWeaponType switch
            {
                "DAGGER_1H"                                                          => 2.3f,
                "SWORD_1H"                                                           => 2.2f,
                "MACE_1H"                                                            => 2.0f,
                "KEYBLADE_2H" or "KEYHAMMER_2H" or "SWORD_2H" or "POLEARM_2H"      => 1.8f,
                "GUN_1H" or "CANNON_2H" or "HARP_2H" or "STAFF_2H" or "BOW"        => 1.7f,
                _                                                                    => 1.5f,
            };
            float critCoeff = Math.Max(1.0f, baseCoeff - (float)Math.Round(sFortitude / 1000.0));
            rawDmg = (int)(rawDmg * critCoeff);
        }

        // Apply NPC level-diff damage reduction (Java: damages *= 1 - getNpcLevelDiffMod(targetLvl-attackerLvl, 0))
        if (target is Npc npcLvlDmg)
        {
            float lvlMod = NpcLevelDiffMod(npcLvlDmg.Level - player.Level);
            if (lvlMod > 0f) rawDmg = Math.Max(1, (int)(rawDmg * (1f - lvlMod)));
        }

        // PvP damage reduction: Java adjustDamages caps PvP damage at 50% of base (damages * 0.50f)
        if (target is Player pvpAtk)
        {
            rawDmg = Math.Max(1, rawDmg / 2);
            // M340: PvP attack/defend ratio — attacker bonus minus defender reduction (Java * 0.001f each)
            int pvpNet = player.PvpAtkRatio - pvpAtk.PvpDefRatio;
            if (pvpNet != 0)
                rawDmg = Math.Max(1, (int)(rawDmg * (1f + pvpNet * 0.001f)));
        }

        // Apply physical defense mitigation; PdefDebuffDelta accumulates statdown reductions (may go negative)
        int pdefBase = target is Player pvpTarget ? pvpTarget.PhysicalDefense
                     : target is Npc npcTarget    ? (npcTarget.Template.Stats?.PDef ?? 0)
                     : 0;
        int pdef   = pdefBase + target.PdefDebuffDelta + target.PdefStatUpDelta;
        int damage = pdef > 0 ? Math.Max(1, rawDmg * 1000 / (1000 + pdef)) : rawDmg;

        // Multi-hit split — Java AttackUtil: player hitCount = Rnd.get(1, weapon.hit_count); split formula same as NPC
        var hitResult = isCrit ? SM_ATTACK.HitResult.Critical : SM_ATTACK.HitResult.Normal;
        int maxHits   = player.MainHandHitCount > 1 ? player.MainHandHitCount : 1;
        int hitCount  = maxHits > 1 ? Random.Shared.Next(1, maxHits + 1) : 1;
        var hits      = new SM_ATTACK.HitEntry[hitCount];
        hits[0] = new(Math.Max(1, (int)(damage * (1f - 0.1f * (hitCount - 1)))), hitResult);
        int otherHit = hitCount > 1 ? Math.Max(1, (int)(damage * 0.1f)) : 0;
        for (int hi = 1; hi < hitCount; hi++)
            hits[hi] = new(otherHit, hitResult);
        int totalDamage = hits.Sum(h => h.Damage);

        // Off-hand dual-wield (Java AttackUtil.calculateOffHandResult): triggered when player has a weapon in slot 2.
        // Off-hand inherits main-hand crit status (getOffHandStats maps CRITICAL→OFFHAND_CRITICAL).
        // 50% damage penalty applied before pdef — approximates dualEffectValue=0 in Java WeaponDualEffect.
        if (player.OffHandMinDmg > 0)
        {
            int ohRaw = Random.Shared.Next(player.OffHandMinDmg, Math.Max(player.OffHandMinDmg + 1, player.OffHandMaxDmg + 1)) + baseAtk;
            // M296: wpndual passive sets DualWieldEffectPct; default 50% penalty when not learned
            int dualPct = player.DualWieldEffectPct > 0 ? player.DualWieldEffectPct : 50;
            ohRaw = ohRaw * dualPct / 100;
            if (isCrit)
            {
                int ohSF = target is Player pvpOhSF ? pvpOhSF.BonusStrikeFortitude + pvpOhSF.StrikeFortitudeDelta : 0;
                float ohCoeff = Math.Max(1.0f, (player.OffHandWeaponType switch
                {
                    "DAGGER_1H" => 2.3f, "SWORD_1H" => 2.2f, "MACE_1H" => 2.0f, _ => 1.5f,
                }) - (float)Math.Round(ohSF / 1000.0));
                ohRaw = (int)(ohRaw * ohCoeff);
            }
            if (target is Npc npcOh) { float m = NpcLevelDiffMod(npcOh.Level - player.Level); if (m > 0f) ohRaw = Math.Max(1, (int)(ohRaw * (1f - m))); }
            else if (target is Player) ohRaw = Math.Max(1, ohRaw / 2); // PvP 50%
            int ohDamage   = pdef > 0 ? Math.Max(1, ohRaw * 1000 / (1000 + pdef)) : Math.Max(1, ohRaw);
            var ohResult   = isCrit ? SM_ATTACK.HitResult.OffHandCritical : SM_ATTACK.HitResult.OffHandNormal;
            int ohMaxHits  = player.OffHandHitCount > 1 ? player.OffHandHitCount : 1;
            int ohHitCount = ohMaxHits > 1 ? Random.Shared.Next(1, ohMaxHits + 1) : 1;
            var ohHits     = new SM_ATTACK.HitEntry[ohHitCount];
            ohHits[0] = new(Math.Max(1, (int)(ohDamage * (1f - 0.1f * (ohHitCount - 1)))), ohResult);
            int ohOther = ohHitCount > 1 ? Math.Max(1, (int)(ohDamage * 0.1f)) : 0;
            for (int i = 1; i < ohHitCount; i++) ohHits[i] = new(ohOther, ohResult);
            hits         = [..hits, ..ohHits];
            totalDamage += ohHits.Sum(h => h.Damage);
        }

        // M260c: route through ApplyDamageAndPublishAsync — applies HP, sets LastCombatTime, publishes DamageDealtEvent
        // M287: suppress DeathEvent in duels — duel restores HP=1 below; the kill is fictional
        bool autoSuppress = target is Player autoPvp && _duelService.GetOpponent(player.ObjectId) == autoPvp.ObjectId;
        await target.ApplyDamageAndPublishAsync(player, totalDamage, DamageKind.AutoAttack, skillId: null, _eventBus, ct, suppressDeathEvent: autoSuppress);

        // Broadcast attack animation then damage report
        await BroadcastAsync(new SM_ATTACK(player, target, attackno: 0, time: (short)_time, type: 0, hits), ct);
        await BroadcastAsync(new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, totalDamage), ct);

        afterAttack:
        // DP gain on successful physical hit (100 DP per attack, capped at 6000)
        if (player.Dp < 6000)
        {
            player.Dp = Math.Min(6000, player.Dp + 100);
            try { await _conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp), ct); } catch { }
        }

        // Godstone proc: main-hand weapon may trigger a secondary skill effect on hit
        // Probability is out of 1000; mirrors Java GodStone.onEquip ActionObserver
        if (target.CurrentHp > 0)
        {
            var mainHand = player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.Slot == 1);
            if (mainHand is { GodStoneItemId: > 0 })
            {
                var godTpl = _dataManager.Items.GetTemplate(mainHand.GodStoneItemId);
                if (godTpl?.Godstone is { } god && god.Probability > 0
                    && Random.Shared.Next(1000) < god.Probability)
                {
                    int procDmg = god.SkillLvl * 20 + Random.Shared.Next(10, 30);
                    bool procSuppress = target is Player procPvp && _duelService.GetOpponent(player.ObjectId) == procPvp.ObjectId;
                    await target.ApplyDamageAndPublishAsync(player, procDmg, DamageKind.PhysicalSkill, god.SkillId, _eventBus, ct, suppressDeathEvent: procSuppress);
                    int procTargetType = target is Npc ? 3 : 0;
                    await BroadcastAsync(new SM_CASTSPELL(player.ObjectId, god.SkillId, god.SkillLvl,
                        procTargetType, target.ObjectId, duration: 0), ct);
                    await BroadcastAsync(new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage,
                        god.SkillId, procDmg), ct);
                }
            }
        }

        // NPC retaliation: force non-aggressive NPCs to engage the player when hit
        if (target is Npc attackedNpc && target.CurrentHp > 0)
            _npcAi.ForceEngage(attackedNpc, player);

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
            deadPlayer.ClearAllEffects();
            await BroadcastAsync(new SM_EMOTION(deadPlayer, EmotionType.DIE), ct);
            await BroadcastAsync(new SM_ABNORMAL_EFFECT(deadPlayer.ObjectId, isPlayer: true), ct);

            var targetConn = _connRegistry.Get(deadPlayer.ObjectId);
            if (targetConn is not null)
            {
                try { await targetConn.SendAsync(new SM_DIE(), ct); } catch { }
                try { await targetConn.SendAsync(SM_SYSTEM_MESSAGE.YouWereKilledBy(player.Name), ct); } catch { }
            }

            // Zone-wide kill announcement: "%0 was killed by %1's attack."
            var killAnnounce = SM_SYSTEM_MESSAGE.PlayerKilledByPlayer(deadPlayer.Name, player.Name);
            int pvpWorldId = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == pvpWorldId)
                    try { await c.SendAsync(killAnnounce, ct); } catch { }

            // Group members see "[player] has died."
            var deadGroup = deadPlayer.Group;
            if (deadGroup is not null)
            {
                var groupDied = SM_SYSTEM_MESSAGE.GroupMemberDied(deadPlayer.Name);
                foreach (var m in deadGroup.Members)
                {
                    if (m.ObjectId == deadPlayer.ObjectId) continue;
                    var mc = _connRegistry.Get(m.ObjectId);
                    if (mc is not null) try { await mc.SendAsync(groupDied, ct); } catch { }
                }
            }

            // PvP Phase 1: AP exchange/rank/persist/legion/quest-notify moved to
            // Combat/Handlers/PvpKillHandler.cs (subscribes to DeathEvent, published by
            // ApplyDamageAndPublishAsync above). The victim's own death packets stay here because
            // that publish already happened before this point — see PvpKillHandler's remarks.
        }
        else if (target is Npc deadNpc)
        {
            deadNpc.State |= CreatureState.Dead;
            await BroadcastAsync(new SM_EMOTION(deadNpc, EmotionType.DIE), ct);

            // DIED shout — NPC death cry
            var diedShout = _dataManager.NpcShouts.GetRandomShout(
                deadNpc.Template.NpcId, NpcShoutData.ShoutEventType.DIED, deadNpc.Position.WorldId);
            if (diedShout.HasValue)
            {
                var shoutPkt  = SM_SYSTEM_MESSAGE.NpcShout(deadNpc.ObjectId, diedShout.Value.StringId);
                int shoutWorld = deadNpc.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == shoutWorld)
                        try { await c.SendAsync(shoutPkt, ct); } catch { }
            }

            _world.Remove(deadNpc);

            // C3 Phase 2 (Java AIQuestion.SHOULD_REWARD): NoAction/Interaction/Trap NPCs never
            // grant XP/loot/AP — only Aggressive/General/Guard do (NpcController.doReward is
            // entirely gated by the same poll in Java).
            bool shouldReward = AiNameRegistry.ShouldReward(deadNpc.Template.Ai);

            // Generate drops keyed by the NPC's objectId (now removed from world)
            if (shouldReward)
                _lootService.GenerateDrops(deadNpc, player);

            // Update quest kill progress for active quests
            await _questService.HandleNpcKillAsync(player, deadNpc, _conn, ct);

            // Award XP — level-diff scaling and group distribution handled inside AddGroupExpAsync
            if (shouldReward)
            {
                long xpBase = deadNpc.Template.Stats?.MaxXp > 0
                    ? deadNpc.Template.Stats.MaxXp
                    : deadNpc.Level * 50L;
                await _expService.AddGroupExpAsync(player, xpBase, deadNpc.Level, ct);
            }

            // Award AP for kills in the Abyss world or against ABYSS_GUARD NPCs; persist immediately
            if (shouldReward && (deadNpc.Position.WorldId == AbyssRankService.AbyssWorldId
                || deadNpc.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase)))
            {
                int ap = AbyssRankService.CalculateNpcApReward(deadNpc.Level);
                if (player.APBoostDelta != 0)
                    ap = Math.Max(1, ap * (100 + player.APBoostDelta) / 100);
                bool npcRankUp = AbyssRankService.AddAp(player, ap);
                if (player.AbyssRank > player.AbyssMaxRank)
                    player.AbyssMaxRank = player.AbyssRank;
                await _conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), ct);
                if (npcRankUp)
                    await BroadcastAsync(new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank), ct);
                await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, ct);
                await AwardLegionContributionAsync(player, ap, ct);
            }

            // Decay timing mirrors Java RespawnService: 5s (no drops registered), 90s (empty), 300s (with drops)
            var pendingDrops = _lootService.GetLoot(deadNpc.ObjectId);
            int decayMs = pendingDrops is null ? 5_000 : pendingDrops.Count == 0 ? 90_000 : 300_000;

            var registry  = _connRegistry;
            var spawnSvc  = _spawnService;
            var lootSvc   = _lootService;
            int npcWorldId = deadNpc.Position.WorldId;
            _ = Task.Run(async () =>
            {
                await Task.Delay(decayMs);
                var del = new SM_DELETE(deadNpc.ObjectId);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                        try { await c.SendAsync(del); } catch { }
                lootSvc.ClearLoot(deadNpc.ObjectId);
                spawnSvc.ScheduleRespawn(deadNpc);
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

    // Java NpcGameStats.calcStats(): level*(33.6-0.16*level)+5; used as base for both evasion and physical accuracy
    private static int NpcPhysicalAccuracy(Model.Npc npc)
        => (int)Math.Round(npc.Level * (33.6 - 0.16 * npc.Level) + 5);

    // Java StatFunctions.getNpcLevelDiffMod: multiplier applied to dodge rate and damage when NPC > player level
    private static float NpcLevelDiffMod(int levelDiff) => levelDiff switch
    {
        3  => 0.1f, 4 => 0.2f, 5 => 0.3f, 6 => 0.4f,
        7  => 0.5f, 8 => 0.6f, 9 => 0.7f,
        _  => levelDiff > 9 ? 0.8f : 0f
    };

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
