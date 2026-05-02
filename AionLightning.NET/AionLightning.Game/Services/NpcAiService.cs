using System.Collections.Concurrent;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Background service that ticks every 2 seconds, handles NPC aggro/combat and idle wander.
/// Aggressive NPCs (AggroRange > 0, Ai != "dummy") engage nearby players; idle NPCs wander
/// within WanderRadius units of their spawn point.
/// NPCs chase targets: move toward the player when outside melee range, attack when within range.
/// </summary>
public sealed class NpcAiService : BackgroundService
{
    private static readonly TimeSpan Interval              = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DefaultAttackCooldown = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan WanderCooldown        = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan IdleShoutCooldown     = TimeSpan.FromSeconds(30);
    private const float ChaseTargetRange  = 50f;   // Java AiInfo default chase_target
    private const float ChaseHomeRange   = 200f;  // Java AiInfo default chase_home
    private const float WanderRadius     = 5.0f;
    private const float WanderSpeed       = 1.5f;
    private const float ChaseSpeed        = 6.0f;
    private const float MeleeRange        = 2.5f;
    private const int   NpcSkillCooldownMs = 8000;

    private sealed record WanderState(float Tx, float Ty, float Tz, DateTime ArrivalTime);
    private sealed record ChaseState(float Tx, float Ty, float Tz, DateTime ArrivalTime);
    private sealed record ReturnState(float Tx, float Ty, float Tz, DateTime ArrivalTime);

    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;
    private readonly ExperienceService _expService;
    private readonly ILogger<NpcAiService> _log;
    private readonly RateOptions _rates;
    private readonly Dictionary<int, DateTime>    _lastAttackTime  = new();
    private readonly Dictionary<int, DateTime>    _lastSkillTime   = new();
    private readonly ConcurrentDictionary<int, int> _npcTargets    = new();
    private readonly Dictionary<int, WanderState> _wanderState     = new();
    private readonly Dictionary<int, DateTime>    _lastWanderTime  = new();
    private readonly Dictionary<int, ChaseState>  _chaseState      = new();
    private readonly Dictionary<int, ReturnState> _returnState     = new();
    // Walker patrol: NPC objectId → current route step index
    private readonly Dictionary<int, int>         _walkerStepIndex  = new();
    // Idle shout: NPC objectId → last shout time
    private readonly Dictionary<int, DateTime>    _lastIdleShoutTime  = new();
    // ATTACK_BEGIN shout: objectIds that have already fired the shout in current combat
    private readonly HashSet<int>                 _attackBegunNpcs    = new();
    // Out-of-combat HP regen: NPC objectId → last regen timestamp
    private readonly Dictionary<int, DateTime>    _lastRegenTime      = new();

    public NpcAiService(GameWorld world, PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ExperienceService expService, ILogger<NpcAiService> log, IOptions<RateOptions> rates)
    {
        _world        = world;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
        _expService   = expService;
        _log          = log;
        _rates        = rates.Value;
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
            _lastSkillTime.Clear();
            _chaseState.Clear();
            _returnState.Clear();
            _attackBegunNpcs.Clear();
            return;
        }

        foreach (var npc in _world.GetAllNpcs())
        {
            if (npc.IsAlreadyDead)
            {
                _lastAttackTime.Remove(npc.ObjectId);
                _lastSkillTime.Remove(npc.ObjectId);
                _npcTargets.TryRemove(npc.ObjectId, out _);
                _wanderState.Remove(npc.ObjectId);
                _lastWanderTime.Remove(npc.ObjectId);
                _chaseState.Remove(npc.ObjectId);
                _returnState.Remove(npc.ObjectId);
                _walkerStepIndex.Remove(npc.ObjectId);
                _lastIdleShoutTime.Remove(npc.ObjectId);
                _attackBegunNpcs.Remove(npc.ObjectId);
                _lastRegenTime.Remove(npc.ObjectId);
                npc.Target = null;
                continue;
            }
            bool isDummy = string.Equals(npc.Template.Ai, "dummy", StringComparison.OrdinalIgnoreCase);

            // Out-of-combat HP regen — Java LifeStatsRestoreService: MaxHp/4 every 6s, 1.7s initial delay
            if (!_npcTargets.ContainsKey(npc.ObjectId) && npc.CurrentHp < npc.MaxHp && npc.MaxHp > 0)
            {
                var regenNow = DateTime.UtcNow;
                if ((regenNow - npc.LastCombatTime).TotalMilliseconds >= 1700
                    && (!_lastRegenTime.TryGetValue(npc.ObjectId, out var lastRegen)
                        || (regenNow - lastRegen).TotalSeconds >= 6.0))
                {
                    int heal = Math.Max(1, npc.MaxHp / 4);
                    npc.CurrentHp = Math.Min(npc.MaxHp, npc.CurrentHp + heal);
                    _lastRegenTime[npc.ObjectId] = regenNow;

                    var regenPkt  = new SM_ATTACK_STATUS(npc, SM_ATTACK_STATUS.AttackType.NaturalHp, 0, heal);
                    int regenWorld = npc.Position.WorldId;
                    foreach (var conn in _connRegistry.GetAll())
                        if (conn.ActivePlayer?.Position.WorldId == regenWorld)
                            try { await conn.SendAsync(regenPkt, ct); } catch { }
                }
            }

            // If returning home, snap position on arrival; skip all AI until home
            if (_returnState.TryGetValue(npc.ObjectId, out var rs))
            {
                if (DateTime.UtcNow >= rs.ArrivalTime)
                {
                    npc.Position = npc.Position with { X = rs.Tx, Y = rs.Ty, Z = rs.Tz };
                    _returnState.Remove(npc.ObjectId);
                }
                else
                {
                    continue; // still returning — no aggro, no wander this tick
                }
            }

            // Aggro + combat
            Player? target = null;
            if (!isDummy && npc.Template.AggroRange > 0)
            {
                // Validate locked target — clear if dead, wrong world, out of chase range, NPC too far from home, or idle timeout
                if (_npcTargets.TryGetValue(npc.ObjectId, out int lockedId))
                {
                    var locked = players.FirstOrDefault(p => p.ObjectId == lockedId);
                    bool idleTimeout = (DateTime.UtcNow - npc.LastCombatTime).TotalSeconds > 20;
                    if (locked is not null
                        && !locked.IsAlreadyDead
                        && !idleTimeout
                        && locked.Position.WorldId == npc.HomePosition.WorldId
                        && npc.Position.DistanceTo(locked.Position) <= ChaseTargetRange
                        && npc.HomePosition.DistanceTo(npc.Position) <= ChaseHomeRange)
                    {
                        target = locked;
                    }
                    else
                    {
                        // Lost target — stop any chase and return home
                        await StopChaseAsync(npc, ct);
                        _npcTargets.TryRemove(npc.ObjectId, out _);
                        _attackBegunNpcs.Remove(npc.ObjectId);
                        npc.Target = null;
                        await BroadcastAttackEndShoutAsync(npc, ct);

                        // Transition back to idle stance
                        int leashWorld  = npc.Position.WorldId;
                        var neutralMode = new SM_EMOTION(npc, EmotionType.NEUTRALMODE);
                        foreach (var conn in _connRegistry.GetAll())
                            if (conn.ActivePlayer?.Position.WorldId == leashWorld)
                                try { await conn.SendAsync(neutralMode, ct); } catch { }
                    }
                }

                // No locked target — scan from NPC's current position for nearest player in aggro range
                if (target is null)
                {
                    float minDist = float.MaxValue;
                    foreach (var player in players)
                    {
                        if (player.IsAlreadyDead) continue;
                        if (player.Position.WorldId != npc.Position.WorldId) continue;
                        if (!_dataManager.Tribes.IsAggressiveToPlayer(npc.Template.Tribe, player.Race)) continue;

                        float dist = npc.Position.DistanceTo(player.Position);
                        if (dist <= npc.Template.AggroRange && dist < minDist)
                        {
                            minDist = dist;
                            target  = player;
                        }
                    }
                    if (target is not null)
                    {
                        _npcTargets[npc.ObjectId] = target.ObjectId;
                        npc.Target = target;
                        AlertNearbyAllies(npc, target);

                        // Transition to combat stance
                        int engageWorld = npc.Position.WorldId;
                        var attackMode  = new SM_EMOTION(npc, EmotionType.ATTACKMODE);
                        foreach (var conn in _connRegistry.GetAll())
                            if (conn.ActivePlayer?.Position.WorldId == engageWorld)
                                try { await conn.SendAsync(attackMode, ct); } catch { }

                        // SEE shout — NPC has just spotted a player
                        var shout = _dataManager.NpcShouts.GetRandomShout(
                            npc.Template.NpcId, NpcShoutData.ShoutEventType.SEE, npc.Position.WorldId);
                        if (shout.HasValue)
                        {
                            var shoutPkt  = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, shout.Value.StringId);
                            int shoutWorld = npc.Position.WorldId;
                            foreach (var conn in _connRegistry.GetAll())
                                if (conn.ActivePlayer?.Position.WorldId == shoutWorld)
                                    try { await conn.SendAsync(shoutPkt, ct); } catch { }
                        }
                    }
                }
            }

            // Wander when idle (no aggro target and not a dummy)
            if (target is null && !isDummy)
                await WanderAsync(npc, ct);

            if (target is null) continue;

            // Advance any ongoing chase — update NPC position on arrival
            if (_chaseState.TryGetValue(npc.ObjectId, out var cs) && DateTime.UtcNow >= cs.ArrivalTime)
            {
                npc.Position = npc.Position with { X = cs.Tx, Y = cs.Ty, Z = cs.Tz };
                _chaseState.Remove(npc.ObjectId);
            }

            float distToTarget = npc.Position.DistanceTo(target.Position);

            // Outside melee range — chase the target
            if (distToTarget > MeleeRange)
            {
                await ChaseAsync(npc, target, ct);
                continue; // don't attack this tick while chasing
            }

            // Within melee range — stop any chase animation, then attack
            if (_chaseState.ContainsKey(npc.ObjectId))
            {
                var stopPkt = SM_MOVE.StopNpcMove(npc.ObjectId,
                    npc.Position.X, npc.Position.Y, npc.Position.Z, (byte)npc.Position.Heading);
                int stopWorld = npc.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == stopWorld)
                        try { await c.SendAsync(stopPkt, ct); } catch { }
                _chaseState.Remove(npc.ObjectId);
            }

            var now = DateTime.UtcNow;
            int atkDelayMs = npc.Template.AttackDelay > 0 ? npc.Template.AttackDelay : (int)DefaultAttackCooldown.TotalMilliseconds;
            if ((now - _lastAttackTime.GetValueOrDefault(npc.ObjectId)).TotalMilliseconds < atkDelayMs) continue;
            _lastAttackTime[npc.ObjectId] = now;

            // ATTACK_BEGIN shout — fires once on the NPC's first attack in each combat engagement
            if (_attackBegunNpcs.Add(npc.ObjectId))
            {
                var atkBeginShout = _dataManager.NpcShouts.GetRandomShout(
                    npc.Template.NpcId, NpcShoutData.ShoutEventType.ATTACK_BEGIN, npc.Position.WorldId);
                if (atkBeginShout.HasValue)
                {
                    var shoutPkt  = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, atkBeginShout.Value.StringId);
                    int shoutWorld = npc.Position.WorldId;
                    foreach (var conn in _connRegistry.GetAll())
                        if (conn.ActivePlayer?.Position.WorldId == shoutWorld)
                            try { await conn.SendAsync(shoutPkt, ct); } catch { }
                }
            }

            // Java NpcController.attack() calls canAttack() which checks CANT_ATTACK_STATE
            if ((npc.ActiveCcFlags & AbnormalCcFlags.CantAttack) != 0) continue;

            // Deal damage — both NPC and target enter combat (suppresses regen for both)
            int baseAtk = (npc.Template.Stats?.MainHandAttack ?? 0) + npc.PatkDebuffDelta;
            int rawDmg  = baseAtk > 0
                ? Math.Max(1, baseAtk + Random.Shared.Next(-(baseAtk / 4), baseAtk / 4 + 1))
                : Math.Max(1, npc.Level * 5 + Random.Shared.Next(5, 20));
            if (_rates.NormalMobsRatePw != 1.0)
                rawDmg = Math.Max(1, (int)(rawDmg * _rates.NormalMobsRatePw));

            // Dodge / parry / block checks against player targets (mirrors CM_ATTACK player-vs-NPC checks)
            int npcWorld  = npc.Position.WorldId;
            int npcAccuracy = (int)Math.Round(npc.Level * (33.6 - 0.16 * npc.Level) + 5)
                            + (npc.Template.Stats?.MainHandAccuracy ?? 0);
            var hitResult = SM_ATTACK.HitResult.Normal;

            if (target is Player pvpDef)
            {
                int ev = pvpDef.BaseEvasion + pvpDef.BonusEvasion;
                float dodgeChance = Math.Clamp((ev - npcAccuracy) * 0.6f + 50f, 0f, 300f);
                if (Random.Shared.Next(1000) < (int)dodgeChance)
                {
                    var dodgePkt = new SM_ATTACK(npc, target, attackno: 0, time: 0, type: 0, damage: 0, SM_ATTACK.HitResult.Dodge);
                    foreach (var conn in _connRegistry.GetAll())
                        if (conn.ActivePlayer?.Position.WorldId == npcWorld)
                            try { await conn.SendAsync(dodgePkt, ct); } catch { }
                    continue;
                }

                int par = pvpDef.BaseParry + pvpDef.BonusParry + pvpDef.ParryDelta;
                if (par > 0)
                {
                    float parryChance = Math.Clamp((par - npcAccuracy) * 0.6f + 50f, 0f, 400f);
                    if (Random.Shared.Next(1000) < (int)parryChance)
                    {
                        rawDmg    = Math.Max(1, (int)(rawDmg * 0.6f));
                        hitResult = SM_ATTACK.HitResult.Parry;
                    }
                }

                if (hitResult == SM_ATTACK.HitResult.Normal)
                {
                    int blk = pvpDef.BaseBlock + pvpDef.BonusBlock + pvpDef.BlockDelta;
                    if (blk > 0)
                    {
                        float blockChance = Math.Clamp(blk - npcAccuracy, 0f, 500f);
                        if (Random.Shared.Next(1000) < (int)blockChance)
                        {
                            rawDmg    = Math.Max(1, (int)(rawDmg * 0.5f));
                            hitResult = SM_ATTACK.HitResult.Block;
                        }
                    }
                }
            }

            // NPC critical hit — Java AttackUtil.calculateWeaponCritical(weaponType=null) → coef=2.0; fortitude reduces by round(fortitude/1000)
            int npcCritRating = (npc.Template.Stats?.Power > 0 ? npc.Template.Stats.Power : 10) + npc.PhysCritDelta;
            double npcCritRate = npcCritRating <= 440 ? npcCritRating * 0.1
                               : npcCritRating <= 600 ? 44.0 + (npcCritRating - 440) * 0.05
                               : 52.0 + (npcCritRating - 600) * 0.02;
            if (Random.Shared.Next(100) < (int)npcCritRate)
            {
                int sFortitude = target is Player pvpSF ? pvpSF.BonusStrikeFortitude + pvpSF.StrikeFortitudeDelta : 0;
                float critCoeff = Math.Max(1.0f, 2.0f - (float)Math.Round(sFortitude / 1000.0));
                rawDmg = (int)(rawDmg * critCoeff);
                if (hitResult == SM_ATTACK.HitResult.Normal)
                    hitResult = SM_ATTACK.HitResult.Critical;
            }

            // Apply physical defense mitigation (diminishing returns: pdef / (pdef + 1000))
            int pdef   = target is Player tp ? tp.PhysicalDefense : 0;
            int damage = pdef > 0 ? Math.Max(1, rawDmg * 1000 / (1000 + pdef)) : rawDmg;

            // Multi-hit split — Java AttackUtil: NPC hits = Rnd.get(1,3); first = damage*(1-0.1*(n-1)), rest = damage*0.1
            int hitCount = Random.Shared.Next(1, 4);
            var hits = new SM_ATTACK.HitEntry[hitCount];
            hits[0] = new(Math.Max(1, (int)(damage * (1f - 0.1f * (hitCount - 1)))), hitResult);
            int otherHit = hitCount > 1 ? Math.Max(1, (int)(damage * 0.1f)) : 0;
            for (int hi = 1; hi < hitCount; hi++)
                hits[hi] = new(otherHit, hitResult);
            int totalDamage = hits.Sum(h => h.Damage);

            target.CurrentHp      = Math.Max(0, target.CurrentHp - totalDamage);
            target.LastCombatTime = now;
            npc.LastCombatTime    = now;

            var attackPkt = new SM_ATTACK(npc, target, attackno: 0, time: 0, type: 0, hits);
            var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, totalDamage);
            foreach (var conn in _connRegistry.GetAll())
            {
                if (conn.ActivePlayer?.Position.WorldId != npcWorld) continue;
                try { await conn.SendAsync(attackPkt, ct); } catch { /* ignore */ }
                try { await conn.SendAsync(statusPkt, ct); } catch { /* ignore */ }
            }

            await BroadcastGroupHpAsync(target, ct);

            // ATTACK shout — fires on subsequent attacks with ~20% probability
            if (Random.Shared.Next(100) < 20)
            {
                var atkShout = _dataManager.NpcShouts.GetRandomShout(
                    npc.Template.NpcId, NpcShoutData.ShoutEventType.ATTACK, npc.Position.WorldId);
                if (atkShout.HasValue)
                {
                    var shoutPkt = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, atkShout.Value.StringId);
                    foreach (var conn in _connRegistry.GetAll())
                        if (conn.ActivePlayer?.Position.WorldId == npcWorld)
                            try { await conn.SendAsync(shoutPkt, ct); } catch { }
                }
            }

            // Occasional NPC skill use (independent of melee cooldown)
            if (target.CurrentHp > 0)
                await TryCastNpcSkillAsync(npc, target, now, npcWorld, ct);

            if (target.CurrentHp > 0) continue;

            // Player killed by NPC — clear their target lock so NPC idles afterward
            _npcTargets.TryRemove(npc.ObjectId, out _);
            _chaseState.Remove(npc.ObjectId);
            _lastSkillTime.Remove(npc.ObjectId);
            _attackBegunNpcs.Remove(npc.ObjectId);
            npc.Target = null;
            await BroadcastAttackEndShoutAsync(npc, ct);

            target.State |= CreatureState.Dead;
            target.ClearAllEffects();
            var diePkt      = new SM_EMOTION(target, EmotionType.DIE);
            var clearEffect = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer: true);
            foreach (var conn in _connRegistry.GetAll())
            {
                if (conn.ActivePlayer?.Position.WorldId != npcWorld) continue;
                try { await conn.SendAsync(diePkt, ct); } catch { /* ignore */ }
                try { await conn.SendAsync(clearEffect, ct); } catch { /* ignore */ }
            }

            var targetConn = _connRegistry.Get(target.ObjectId);
            if (targetConn is not null)
            {
                try { await targetConn.SendAsync(new SM_DIE(), ct); } catch { /* ignore */ }
                try { await targetConn.SendAsync(SM_SYSTEM_MESSAGE.YouWereKilledBy(npc.Template.Name), ct); } catch { }
            }

            // Group members see "[player] has died."
            var group = target.Group;
            if (group is not null)
            {
                var groupDied = SM_SYSTEM_MESSAGE.GroupMemberDied(target.Name);
                foreach (var m in group.Members)
                {
                    if (m.ObjectId == target.ObjectId) continue;
                    var mc = _connRegistry.Get(m.ObjectId);
                    if (mc is not null) try { await mc.SendAsync(groupDied, ct); } catch { }
                }
            }

            // Death XP loss — 0.25% of expNeeded for level 50+ (mirrors Java XPLossEnum)
            if (targetConn is not null)
            {
                long xpLost = _expService.ApplyDeathXpLoss(target, _dataManager);
                if (xpLost > 0)
                {
                    long expNeeded = _dataManager.ExpTable.GetStartExpForLevel(target.Level + 1);
                    try { await targetConn.SendAsync(new SM_STATUPDATE_EXP(target.Exp, target.ExpRecoverable, expNeeded), ct); } catch { }
                }
            }
        }
    }

    private async Task TryCastNpcSkillAsync(Npc npc, Player target, DateTime now, int worldId, CancellationToken ct)
    {
        if ((npc.ActiveCcFlags & AbnormalCcFlags.CantAttack) != 0) return;
        if ((now - _lastSkillTime.GetValueOrDefault(npc.ObjectId)).TotalMilliseconds < NpcSkillCooldownMs) return;

        var skills = _dataManager.NpcSkills.GetSkills(npc.Template.NpcId);
        if (skills is null || skills.Count == 0) return;

        int npcHpPct = npc.HpPercentage;
        var eligible = skills.Where(s => s.IsReadyForNpcHp(npcHpPct)).ToList();
        if (eligible.Count == 0) return;

        var entry = eligible[Random.Shared.Next(eligible.Count)];
        if (Random.Shared.Next(100) >= entry.Probability) return;

        _lastSkillTime[npc.ObjectId] = now;

        var castPkt = new SM_CASTSPELL(npc.ObjectId, entry.SkillId, entry.SkillLevel, 3, target.ObjectId, 0);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(castPkt, ct); } catch { }

        var skillTemplate = _dataManager.Skills.GetTemplate(entry.SkillId);
        // Mirror M194: slow/snare/statdown skills have duration="0" on the template;
        // actual duration comes from the effect element's duration2 attribute.
        int effectiveDuration = skillTemplate is not null
            ? (skillTemplate.Duration > 0 ? skillTemplate.Duration : skillTemplate.Effects?.EffectDuration ?? 0)
            : 0;
        switch (skillTemplate?.SubType)
        {
            case SkillSubType.HEAL:
                await CastNpcHealAsync(npc, entry.SkillId, now, worldId, ct);
                break;
            case SkillSubType.BUFF or SkillSubType.CHANT:
                await CastNpcBuffAsync(npc, entry.SkillId, entry.SkillLevel, effectiveDuration, now, worldId, ct);
                break;
            case SkillSubType.DEBUFF:
                await CastNpcDebuffAsync(npc, target, entry.SkillId, entry.SkillLevel, effectiveDuration,
                    skillTemplate.CcFlags,
                    skillTemplate.Effects?.SnareSpeedPct      ?? 0,
                    skillTemplate.Effects?.SlowAttackSpeedPct ?? 0,
                    skillTemplate.Effects?.PdefAddDelta        ?? 0,
                    skillTemplate.Effects?.MResistAddDelta     ?? 0,
                    skillTemplate.Effects?.PhysAtkAddDelta     ?? 0,
                    skillTemplate.Effects?.EvasionAddDelta     ?? 0,
                    skillTemplate.Effects?.MaxHpAddDelta       ?? 0,
                    skillTemplate.Effects?.MagicAtkAddDelta    ?? 0,
                    skillTemplate.Effects?.AtkSpeedAddDelta    ?? 0,
                    skillTemplate.Effects?.MaxMpAddDelta       ?? 0,
                    skillTemplate.Effects?.MagicBoostAddDelta  ?? 0,
                    skillTemplate.Effects?.PhysAccAddDelta     ?? 0,
                    skillTemplate.Effects?.MagicAccAddDelta    ?? 0,
                    skillTemplate.Effects?.ParryAddDelta       ?? 0,
                    skillTemplate.Effects?.BlockAddDelta       ?? 0,
                    skillTemplate.Effects?.PhysCritAddDelta         ?? 0,
                    skillTemplate.Effects?.MagicCritAddDelta        ?? 0,
                    skillTemplate.Effects?.PhysCritResistAddDelta   ?? 0,
                    skillTemplate.Effects?.MagicCritResistAddDelta  ?? 0,
                    skillTemplate.Effects?.StrikeFortitudeAddDelta  ?? 0,
                    skillTemplate.Effects?.SpellFortitudeAddDelta   ?? 0,
                    now, worldId, ct);
                break;
            default:
                await CastNpcDamageAsync(npc, target, entry.SkillId, skillTemplate, now, worldId, ct);
                break;
        }

        var activationPkt = new SM_SKILL_ACTIVATION(entry.SkillId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(activationPkt, ct); } catch { }
    }

    private async Task CastNpcDamageAsync(Npc npc, Player target, int skillId,
        SkillTemplate? skillTemplate, DateTime now, int worldId, CancellationToken ct)
    {
        // Primary target hit
        int rawSpellDmg = Math.Max(1, npc.Level * 8 + Random.Shared.Next(10, 40));
        int mdef        = target.MagicDefense;
        int spellDmg    = mdef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + mdef)) : rawSpellDmg;
        target.CurrentHp      = Math.Max(0, target.CurrentHp - spellDmg);
        target.LastCombatTime = now;
        npc.LastCombatTime    = now;

        var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, skillId, spellDmg);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(statusPkt, ct); } catch { }
        await BroadcastGroupHpAsync(target, ct);

        // Caster-centered AoE splash: hit additional players near the NPC
        if (skillTemplate?.IsCasterAoe == true && skillTemplate.EffectiveRange > 0)
        {
            float aoeR   = skillTemplate.EffectiveRange;
            float aoeAlt = Math.Max(1f, skillTemplate.EffectiveAltitude);
            int   maxHits = skillTemplate.TargetMaxCount;
            int   splashCount = 1; // primary target already counted

            foreach (var other in _world.GetAll())
            {
                if (splashCount >= maxHits) break;
                if (other.ObjectId == target.ObjectId) continue;
                if (other.IsAlreadyDead) continue;
                if (other.Position.WorldId != worldId) continue;
                float dx = other.Position.X - npc.Position.X;
                float dy = other.Position.Y - npc.Position.Y;
                float dz = other.Position.Z - npc.Position.Z;
                if (dx * dx + dy * dy > aoeR * aoeR) continue;
                if (Math.Abs(dz) > aoeAlt) continue;

                splashCount++;
                int splashRaw = Math.Max(1, npc.Level * 8 + Random.Shared.Next(10, 40));
                int splashMdef = other.MagicDefense;
                int splashDmg  = splashMdef > 0 ? Math.Max(1, splashRaw * 1000 / (1000 + splashMdef)) : splashRaw;
                other.CurrentHp      = Math.Max(0, other.CurrentHp - splashDmg);
                other.LastCombatTime = now;

                var splashPkt = new SM_ATTACK_STATUS(other, SM_ATTACK_STATUS.AttackType.Damage, skillId, splashDmg);
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(splashPkt, ct); } catch { }
                if (other is Player splashPlayer)
                {
                    await BroadcastGroupHpAsync(splashPlayer, ct);
                    if (splashPlayer.CurrentHp <= 0)
                        await HandleNpcSplashKillAsync(npc, splashPlayer, worldId, ct);
                }
            }
        }
    }

    private async Task HandleNpcSplashKillAsync(Npc npc, Player killed, int worldId, CancellationToken ct)
    {
        killed.State |= CreatureState.Dead;
        killed.ClearAllEffects();

        var diePkt      = new SM_EMOTION(killed, EmotionType.DIE);
        var clearEffect = new SM_ABNORMAL_EFFECT(killed.ObjectId, isPlayer: true);
        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try { await conn.SendAsync(diePkt, ct); } catch { }
            try { await conn.SendAsync(clearEffect, ct); } catch { }
        }

        var killedConn = _connRegistry.Get(killed.ObjectId);
        if (killedConn is not null)
        {
            try { await killedConn.SendAsync(new SM_DIE(), ct); } catch { }
            try { await killedConn.SendAsync(SM_SYSTEM_MESSAGE.YouWereKilledBy(npc.Template.Name), ct); } catch { }
        }

        var group = killed.Group;
        if (group is not null)
        {
            var groupDied = SM_SYSTEM_MESSAGE.GroupMemberDied(killed.Name);
            foreach (var m in group.Members)
            {
                if (m.ObjectId == killed.ObjectId) continue;
                var mc = _connRegistry.Get(m.ObjectId);
                if (mc is not null) try { await mc.SendAsync(groupDied, ct); } catch { }
            }
        }

        if (killedConn is not null)
        {
            long xpLost = _expService.ApplyDeathXpLoss(killed, _dataManager);
            if (xpLost > 0)
            {
                long expNeeded = _dataManager.ExpTable.GetStartExpForLevel(killed.Level + 1);
                try { await killedConn.SendAsync(new SM_STATUPDATE_EXP(killed.Exp, killed.ExpRecoverable, expNeeded), ct); } catch { }
            }
        }
    }

    private async Task CastNpcHealAsync(Npc npc, int skillId, DateTime now, int worldId, CancellationToken ct)
    {
        int healAmt      = Math.Max(1, npc.MaxHp / 6);
        npc.CurrentHp    = Math.Min(npc.MaxHp, npc.CurrentHp + healAmt);
        npc.LastCombatTime = now;

        var statusPkt = new SM_ATTACK_STATUS(npc, SM_ATTACK_STATUS.AttackType.NaturalHp, skillId, healAmt);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(statusPkt, ct); } catch { }
    }

    private async Task CastNpcBuffAsync(Npc npc, int skillId, int skillLevel, int durationMs, DateTime now, int worldId, CancellationToken ct)
    {
        if (durationMs <= 0) return;
        npc.LastCombatTime = now;

        var effect = new AbnormalState { SkillId = skillId, SkillLevel = skillLevel,
            EffectorId = npc.ObjectId, Expiry = DateTime.UtcNow.AddMilliseconds(durationMs) };
        npc.AddEffect(effect);

        var abnormal = new SM_ABNORMAL_EFFECT(npc.ObjectId, isPlayer: false, npc.GetActiveEffects());
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(abnormal, ct); } catch { }

        var expEffect = effect;
        _ = Task.Run(async () =>
        {
            await Task.Delay(durationMs);
            npc.RemoveEffect(expEffect.SkillId, expEffect.Expiry);
            var expired = new SM_ABNORMAL_EFFECT(npc.ObjectId, isPlayer: false, npc.GetActiveEffects());
            int expWorldId = npc.Position.WorldId;
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == expWorldId)
                    try { await conn.SendAsync(expired); } catch { }
        });
    }

    private async Task CastNpcDebuffAsync(Npc npc, Player target, int skillId, int skillLevel, int durationMs,
        AbnormalCcFlags ccFlags, int snareSpeedPct, int slowAtkPct, int pdefDelta, int mresistDelta, int patkDelta, int evasionDelta,
        int maxHpDelta,
        int magicAtkDelta,
        int atkSpdDelta,
        int maxMpDelta,
        int mBoostDebuffDelta,
        int physAccDelta,
        int magicAccDelta,
        int parryDelta,
        int blockDelta,
        int physCritDelta,
        int magicCritDelta,
        int physCritResistDelta,
        int magicCritResistDelta,
        int strikeFortitudeDelta,
        int spellFortitudeDelta,
        DateTime now, int worldId, CancellationToken ct)
    {
        if (durationMs <= 0) return;
        target.LastCombatTime = now;
        npc.LastCombatTime    = now;

        var effect = new AbnormalState { SkillId = skillId, SkillLevel = skillLevel,
            EffectorId = npc.ObjectId, Expiry = DateTime.UtcNow.AddMilliseconds(durationMs),
            CcFlags = ccFlags, IsDebuff = true,
            MovSpeedPct = snareSpeedPct, PreDebuffSpeed = target.MovementSpeed,
            AttackSpeedPct = slowAtkPct, PreDebuffAtkSpeed = target.CurrentAttackSpeed,
            PdefDelta = pdefDelta, MResistDelta = mresistDelta, PatkDelta = patkDelta, EvasionDelta = evasionDelta,
            MaxHpDelta = maxHpDelta, MagicAtkDelta = magicAtkDelta, AtkSpeedDelta = atkSpdDelta,
            MaxMpDelta = maxMpDelta, MagicBoostDeltaVal = mBoostDebuffDelta, PhysAccDeltaVal = physAccDelta,
            MagicAccDeltaVal = magicAccDelta, ParryDeltaVal = parryDelta, BlockDeltaVal = blockDelta,
            PhysCritDeltaVal = physCritDelta, MagicCritDeltaVal = magicCritDelta,
            PhysCritResistDeltaVal = physCritResistDelta, MagicCritResistDeltaVal = magicCritResistDelta,
            StrikeFortitudeDeltaVal = strikeFortitudeDelta, SpellFortitudeDeltaVal = spellFortitudeDelta };
        target.AddEffect(effect);

        // Snare: reduce movement speed; Slow: increase attack speed (higher = slower attacks)
        bool speedChanged = snareSpeedPct != 0 || slowAtkPct != 0;
        if (snareSpeedPct != 0)
            target.MovementSpeed = Math.Max(1.0f, target.MovementSpeed * (100 + snareSpeedPct) / 100f);
        if (slowAtkPct != 0)
            target.CurrentAttackSpeed = Math.Max(500, (int)(target.CurrentAttackSpeed * (100 + slowAtkPct) / 100f));
        if (speedChanged)
        {
            var speedEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(speedEmo, ct); } catch { }
        }

        // StatDown PHYSICAL_DEFENSE / MAGICAL_RESIST / PHYSICAL_ATTACK / EVASION / MAXHP: accumulate deltas; send updated stats panel
        if (pdefDelta    != 0) target.PdefDebuffDelta    += pdefDelta;
        if (mresistDelta != 0) target.MResistDebuffDelta += mresistDelta;
        if (patkDelta    != 0) target.PatkDebuffDelta    += patkDelta;
        if (evasionDelta != 0) target.EvasionDebuffDelta += evasionDelta;
        if (maxHpDelta   != 0)
        {
            target.MaxHpBonusDelta += maxHpDelta;
            int effectiveMax = Math.Max(1, target.MaxHp + target.MaxHpBonusDelta);
            if (target.CurrentHp > effectiveMax) target.CurrentHp = effectiveMax;
        }
        if (magicAtkDelta != 0) target.MagicAtkDebuffDelta += magicAtkDelta;
        if (atkSpdDelta   != 0)
        {
            target.AtkSpeedDebuffDelta += atkSpdDelta;
            var atkSpdEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(atkSpdEmo, ct); } catch { }
        }
        if (maxMpDelta   != 0)
        {
            target.MaxMpBonusDelta += maxMpDelta;
            int effectiveMaxMp = Math.Max(1, target.MaxMp + target.MaxMpBonusDelta);
            if (target.CurrentMp > effectiveMaxMp) target.CurrentMp = effectiveMaxMp;
        }
        if (mBoostDebuffDelta != 0) target.MagicBoostDelta += mBoostDebuffDelta;
        if (physAccDelta      != 0) target.PhysAccDelta    += physAccDelta;
        if (magicAccDelta     != 0) target.MagicAccDelta   += magicAccDelta;
        if (parryDelta        != 0) target.ParryDelta      += parryDelta;
        if (blockDelta        != 0) target.BlockDelta      += blockDelta;
        if (physCritDelta        != 0) target.PhysCritDelta        += physCritDelta;
        if (magicCritDelta       != 0) target.MagicCritDelta       += magicCritDelta;
        if (physCritResistDelta  != 0) target.PhysCritResistDelta  += physCritResistDelta;
        if (magicCritResistDelta != 0) target.MagicCritResistDelta += magicCritResistDelta;
        if (strikeFortitudeDelta != 0) target.StrikeFortitudeDelta += strikeFortitudeDelta;
        if (spellFortitudeDelta  != 0) target.SpellFortitudeDelta  += spellFortitudeDelta;
        if (pdefDelta != 0 || mresistDelta != 0 || patkDelta != 0 || evasionDelta != 0 || maxHpDelta != 0 || magicAtkDelta != 0 || atkSpdDelta != 0 || maxMpDelta != 0 || mBoostDebuffDelta != 0 || physAccDelta != 0 || magicAccDelta != 0 || parryDelta != 0 || blockDelta != 0 || physCritDelta != 0 || magicCritDelta != 0 || physCritResistDelta != 0 || magicCritResistDelta != 0 || strikeFortitudeDelta != 0 || spellFortitudeDelta != 0)
        {
            var statsInfo = new SM_STATS_INFO(target, _dataManager.PlayerStats.GetTemplate(target.PlayerClass, target.Level));
            var dc = _connRegistry.GetAll().FirstOrDefault(c => c.ActivePlayer == target);
            if (dc is not null) try { await dc.SendAsync(statsInfo, ct); } catch { }
        }

        var abnormal = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer: true, target.GetActiveEffects());
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(abnormal, ct); } catch { }

        // Java RootEffect/StunEffect: broadcast SM_TARGET_IMMOBILIZE to freeze the player's position on all clients
        if ((ccFlags & AbnormalCcFlags.CantMove) != 0)
        {
            var immobilize = new SM_TARGET_IMMOBILIZE(target);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(immobilize, ct); } catch { }
        }

        var expEffect = effect;
        _ = Task.Run(async () =>
        {
            await Task.Delay(durationMs);
            // Restore movement speed and attack speed before removing the effect
            bool restored = expEffect.MovSpeedPct != 0 || expEffect.AttackSpeedPct != 0;
            if (expEffect.MovSpeedPct    != 0) target.MovementSpeed      = expEffect.PreDebuffSpeed;
            if (expEffect.AttackSpeedPct != 0) target.CurrentAttackSpeed = expEffect.PreDebuffAtkSpeed;
            if (restored)
            {
                var restoreEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
                int restoreWorld = target.Position.WorldId;
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == restoreWorld)
                        try { await conn.SendAsync(restoreEmo); } catch { }
            }
            // Restore pdef/mresist/patk/evasion/maxhp deltas; send updated stats to player
            if (expEffect.PdefDelta    != 0) target.PdefDebuffDelta    -= expEffect.PdefDelta;
            if (expEffect.MResistDelta != 0) target.MResistDebuffDelta -= expEffect.MResistDelta;
            if (expEffect.PatkDelta    != 0) target.PatkDebuffDelta    -= expEffect.PatkDelta;
            if (expEffect.EvasionDelta != 0) target.EvasionDebuffDelta -= expEffect.EvasionDelta;
            if (expEffect.MaxHpDelta    != 0) target.MaxHpBonusDelta     -= expEffect.MaxHpDelta;
            if (expEffect.MagicAtkDelta != 0) target.MagicAtkDebuffDelta -= expEffect.MagicAtkDelta;
            if (expEffect.AtkSpeedDelta != 0)
            {
                target.AtkSpeedDebuffDelta -= expEffect.AtkSpeedDelta;
                var restoreAtkEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
                int restoreAtkWorld = target.Position.WorldId;
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == restoreAtkWorld)
                        try { await conn.SendAsync(restoreAtkEmo); } catch { }
            }
            if (expEffect.MaxMpDelta        != 0) target.MaxMpBonusDelta  -= expEffect.MaxMpDelta;
            if (expEffect.MagicBoostDeltaVal != 0) target.MagicBoostDelta -= expEffect.MagicBoostDeltaVal;
            if (expEffect.PhysAccDeltaVal    != 0) target.PhysAccDelta    -= expEffect.PhysAccDeltaVal;
            if (expEffect.MagicAccDeltaVal   != 0) target.MagicAccDelta   -= expEffect.MagicAccDeltaVal;
            if (expEffect.ParryDeltaVal      != 0) target.ParryDelta      -= expEffect.ParryDeltaVal;
            if (expEffect.BlockDeltaVal      != 0) target.BlockDelta      -= expEffect.BlockDeltaVal;
            if (expEffect.PhysCritDeltaVal        != 0) target.PhysCritDelta        -= expEffect.PhysCritDeltaVal;
            if (expEffect.MagicCritDeltaVal       != 0) target.MagicCritDelta       -= expEffect.MagicCritDeltaVal;
            if (expEffect.PhysCritResistDeltaVal  != 0) target.PhysCritResistDelta  -= expEffect.PhysCritResistDeltaVal;
            if (expEffect.MagicCritResistDeltaVal != 0) target.MagicCritResistDelta -= expEffect.MagicCritResistDeltaVal;
            if (expEffect.StrikeFortitudeDeltaVal != 0) target.StrikeFortitudeDelta -= expEffect.StrikeFortitudeDeltaVal;
            if (expEffect.SpellFortitudeDeltaVal  != 0) target.SpellFortitudeDelta  -= expEffect.SpellFortitudeDeltaVal;
            if (expEffect.PdefDelta != 0 || expEffect.MResistDelta != 0 || expEffect.PatkDelta != 0 || expEffect.EvasionDelta != 0 || expEffect.MaxHpDelta != 0 || expEffect.MagicAtkDelta != 0 || expEffect.AtkSpeedDelta != 0 || expEffect.MaxMpDelta != 0 || expEffect.MagicBoostDeltaVal != 0 || expEffect.PhysAccDeltaVal != 0 || expEffect.MagicAccDeltaVal != 0 || expEffect.ParryDeltaVal != 0 || expEffect.BlockDeltaVal != 0 || expEffect.PhysCritDeltaVal != 0 || expEffect.MagicCritDeltaVal != 0 || expEffect.PhysCritResistDeltaVal != 0 || expEffect.MagicCritResistDeltaVal != 0 || expEffect.StrikeFortitudeDeltaVal != 0 || expEffect.SpellFortitudeDeltaVal != 0)
            {
                var statsInfo = new SM_STATS_INFO(target, _dataManager.PlayerStats.GetTemplate(target.PlayerClass, target.Level));
                var dc = _connRegistry.GetAll().FirstOrDefault(c => c.ActivePlayer == target);
                if (dc is not null) try { await dc.SendAsync(statsInfo); } catch { }
            }
            target.RemoveEffect(expEffect.SkillId, expEffect.Expiry);
            var expired = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer: true, target.GetActiveEffects());
            int expWorldId = target.Position.WorldId;
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == expWorldId)
                    try { await conn.SendAsync(expired); } catch { }
        });
    }

    private async Task ChaseAsync(Npc npc, Player target, CancellationToken ct)
    {
        float dx = target.Position.X - npc.Position.X;
        float dy = target.Position.Y - npc.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        // Destination: stop just inside melee range
        float stopDist = Math.Max(0, dist - MeleeRange * 0.8f);
        float tx = npc.Position.X + dx / dist * stopDist;
        float ty = npc.Position.Y + dy / dist * stopDist;
        float tz = target.Position.Z;

        float travel  = MathF.Sqrt((tx - npc.Position.X) * (tx - npc.Position.X)
                                  + (ty - npc.Position.Y) * (ty - npc.Position.Y));
        double seconds = travel / ChaseSpeed;
        var arrival   = DateTime.UtcNow.AddSeconds(seconds < 0.1 ? 0.1 : seconds);

        byte heading  = CalcHeading(dx, dy);
        _chaseState[npc.ObjectId] = new ChaseState(tx, ty, tz, arrival);

        var movePkt = SM_MOVE.StartNpcMove(npc.ObjectId,
            npc.Position.X, npc.Position.Y, npc.Position.Z, heading, tx, ty, tz);
        int worldId = npc.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(movePkt, ct); } catch { }
    }

    private async Task StopChaseAsync(Npc npc, CancellationToken ct)
    {
        if (!_chaseState.ContainsKey(npc.ObjectId)) return;
        _chaseState.Remove(npc.ObjectId);

        float dx   = npc.HomePosition.X - npc.Position.X;
        float dy   = npc.HomePosition.Y - npc.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.5f) return;

        byte heading  = CalcHeading(dx, dy);
        double seconds = dist / ChaseSpeed;
        var arrival   = DateTime.UtcNow.AddSeconds(seconds);

        // Track the return so TickAsync snaps position on arrival (no instant snap)
        _returnState[npc.ObjectId] = new ReturnState(
            npc.HomePosition.X, npc.HomePosition.Y, npc.HomePosition.Z, arrival);

        var returnPkt = SM_MOVE.StartNpcMove(npc.ObjectId,
            npc.Position.X, npc.Position.Y, npc.Position.Z, heading,
            npc.HomePosition.X, npc.HomePosition.Y, npc.HomePosition.Z);
        int worldId = npc.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(returnPkt, ct); } catch { }
    }

    private async Task BroadcastAttackEndShoutAsync(Npc npc, CancellationToken ct)
    {
        var shout = _dataManager.NpcShouts.GetRandomShout(
            npc.Template.NpcId, NpcShoutData.ShoutEventType.ATTACK_END, npc.Position.WorldId);
        if (!shout.HasValue) return;
        var pkt     = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, shout.Value.StringId);
        int worldId = npc.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(pkt, ct); } catch { }
    }

    private Task WanderAsync(Npc npc, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(npc.WalkerId))
        {
            var route = _dataManager.Walkers.GetRoute(npc.WalkerId);
            if (route is { Length: > 0 })
                return PatrolAsync(npc, route, ct);
        }
        return WanderRandomAsync(npc, ct);
    }

    private async Task PatrolAsync(Npc npc, WalkerData.RouteStep[] route, CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        int worldId = npc.Position.WorldId;

        // Check if an in-progress wander move has arrived
        if (_wanderState.TryGetValue(npc.ObjectId, out var ws))
        {
            if (now >= ws.ArrivalTime)
            {
                npc.Position = npc.Position with { X = ws.Tx, Y = ws.Ty, Z = ws.Tz };
                var stop = SM_MOVE.StopNpcMove(npc.ObjectId, ws.Tx, ws.Ty, ws.Tz, (byte)npc.Position.Heading);
                _wanderState.Remove(npc.ObjectId);
                // Advance to next step (cycle)
                int prev = _walkerStepIndex.GetValueOrDefault(npc.ObjectId, 0);
                _walkerStepIndex[npc.ObjectId] = (prev + 1) % route.Length;
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(stop, ct); } catch { }
            }
            return; // still traveling
        }

        // Start moving to the current step
        int stepIdx = _walkerStepIndex.GetValueOrDefault(npc.ObjectId, 0);
        var step    = route[stepIdx % route.Length];

        float dx = step.X - npc.Position.X;
        float dy = step.Y - npc.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.5f)
        {
            // Already at this step — advance immediately without broadcasting a zero-distance move
            _walkerStepIndex[npc.ObjectId] = (stepIdx + 1) % route.Length;
            return;
        }

        float travelTime = dist / WanderSpeed;
        var   arrival    = now.AddSeconds(travelTime < 0.5 ? 0.5 : travelTime);
        byte  heading    = CalcHeading(dx, dy);

        _wanderState[npc.ObjectId] = new WanderState(step.X, step.Y, step.Z, arrival);

        var movePkt = SM_MOVE.StartNpcMove(npc.ObjectId,
            npc.Position.X, npc.Position.Y, npc.Position.Z, heading, step.X, step.Y, step.Z);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(movePkt, ct); } catch { }
    }

    private async Task WanderRandomAsync(Npc npc, CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        int worldId = npc.HomePosition.WorldId;

        // If there's an active wander, check arrival
        if (_wanderState.TryGetValue(npc.ObjectId, out var ws))
        {
            if (now >= ws.ArrivalTime)
            {
                // Arrived — update NPC position and broadcast stop
                npc.Position = npc.Position with { X = ws.Tx, Y = ws.Ty, Z = ws.Tz };
                var stop = SM_MOVE.StopNpcMove(npc.ObjectId, ws.Tx, ws.Ty, ws.Tz, (byte)npc.Position.Heading);
                _wanderState.Remove(npc.ObjectId);
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(stop, ct); } catch { }
            }
            return; // still traveling, or just arrived (done for this tick)
        }

        // Cool-down before picking a new wander target
        if (now - _lastWanderTime.GetValueOrDefault(npc.ObjectId) < WanderCooldown) return;

        // Pick a random destination within WanderRadius of HomePosition
        float angle = (float)(Random.Shared.NextDouble() * 2 * Math.PI);
        float dist  = (float)(Random.Shared.NextDouble() * WanderRadius);
        float tx    = npc.HomePosition.X + MathF.Cos(angle) * dist;
        float ty    = npc.HomePosition.Y + MathF.Sin(angle) * dist;
        float tz    = npc.HomePosition.Z;

        float travel  = MathF.Sqrt((tx - npc.Position.X) * (tx - npc.Position.X)
                                  + (ty - npc.Position.Y) * (ty - npc.Position.Y));
        float seconds = travel / WanderSpeed;
        var arrival   = now.AddSeconds(seconds < 0.5 ? 0.5 : seconds);

        byte heading  = CalcHeading(tx - npc.Position.X, ty - npc.Position.Y);

        _wanderState[npc.ObjectId]    = new WanderState(tx, ty, tz, arrival);
        _lastWanderTime[npc.ObjectId] = now;

        var start = SM_MOVE.StartNpcMove(npc.ObjectId, npc.Position.X, npc.Position.Y, npc.Position.Z,
            heading, tx, ty, tz);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(start, ct); } catch { }

        // IDLE shout — periodic ambient NPC shout (at most once per 30 s)
        if (now - _lastIdleShoutTime.GetValueOrDefault(npc.ObjectId) >= IdleShoutCooldown)
        {
            var idleShout = _dataManager.NpcShouts.GetRandomShout(
                npc.Template.NpcId, NpcShoutData.ShoutEventType.IDLE, worldId);
            if (idleShout.HasValue)
            {
                _lastIdleShoutTime[npc.ObjectId] = now;
                var shoutPkt = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, idleShout.Value.StringId);
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(shoutPkt, ct); } catch { }
            }
        }
    }

    /// <summary>
    /// Called from packet handlers when a player hits an NPC directly.
    /// Forces the NPC to engage the player even if it is outside its natural aggro range.
    /// Thread-safe: uses ConcurrentDictionary for target registration.
    /// </summary>
    public void ForceEngage(Npc npc, Player player)
    {
        if (npc.IsAlreadyDead) return;
        npc.LastCombatTime = DateTime.UtcNow; // refresh on every hit so idle timeout resets
        if (_npcTargets.TryAdd(npc.ObjectId, player.ObjectId))
        {
            npc.Target = player;

            // Broadcast combat stance — fire-and-forget since ForceEngage is sync
            int engageWorld = npc.Position.WorldId;
            var attackMode  = new SM_EMOTION(npc, EmotionType.ATTACKMODE);
            _ = Task.Run(async () =>
            {
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == engageWorld)
                        try { await conn.SendAsync(attackMode); } catch { }
            });
        }
    }

    // When an NPC engages a player, notify nearby same-tribe NPCs to also engage.
    // Uses each potential ally's own aggro range as the assist radius.
    private void AlertNearbyAllies(Npc aggressor, Player target)
    {
        int worldId = aggressor.Position.WorldId;
        foreach (var ally in _world.GetAllNpcs())
        {
            if (ally.ObjectId == aggressor.ObjectId) continue;
            if (ally.IsAlreadyDead) continue;
            if (ally.Position.WorldId != worldId) continue;
            if (_npcTargets.ContainsKey(ally.ObjectId)) continue;
            if (string.Equals(ally.Template.Ai, "dummy", StringComparison.OrdinalIgnoreCase)) continue;
            if (!_dataManager.Tribes.IsSupport(ally.Template.Tribe, aggressor.Template.Tribe)) continue;

            float checkRange = ally.Template.AggroRange > 0 ? ally.Template.AggroRange : MeleeRange * 4;
            if (ally.Position.DistanceTo(aggressor.Position) > checkRange) continue;

            _npcTargets[ally.ObjectId] = target.ObjectId;
            ally.Target = target;
        }
    }

    private static byte CalcHeading(float dx, float dy)
        => (byte)((int)(MathF.Atan2(dy, dx) * (128.0f / MathF.PI) + 64) & 0xFF);

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
