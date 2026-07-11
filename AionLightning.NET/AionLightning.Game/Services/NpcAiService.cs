using System.Collections.Concurrent;
using AionLightning.Commons.Events;
using AionLightning.Game.Combat;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Ai;
using AionLightning.Game.Model.Summons;
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
/// Combat mechanics (crit/parry/block/DoT/AoE/NPC skills) are the same for every NPC; WHETHER an
/// NPC proactively aggro-scans, wanders/patrols, or retaliates when attacked is decided by its
/// <see cref="AiArchetype"/> (see <see cref="AiNameRegistry"/> and <see cref="ResolveArchetype"/>):
/// Aggressive scans + wanders + retaliates, General only retaliates + wanders, Guard scans + fights
/// + retaliates like Aggressive but never random-wanders off its post (walker routes still apply),
/// NoAction/Interaction are fully inert, and Trap runs its own proximity-trigger tick (see
/// <see cref="TickTrapAsync"/>) instead of the aggro/combat/wander pipeline below. NPCs chase
/// targets: move toward the player when outside melee range, attack when within range.
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
    // M381: summon AI tuning — GUARD follow trigger distance, ATTACK melee range, melee attack cadence
    private const float SummonFollowRange     = 4.0f;
    private const float SummonMeleeRange      = 2.5f;
    private const int   SummonAttackCooldownMs = 1500;

    private sealed record WanderState(float Tx, float Ty, float Tz, DateTime ArrivalTime);
    private sealed record ChaseState(float Tx, float Ty, float Tz, DateTime ArrivalTime);
    private sealed record ReturnState(float Tx, float Ty, float Tz, DateTime ArrivalTime);

    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;
    private readonly ExperienceService _expService;
    private readonly ILogger<NpcAiService> _log;
    private readonly RateOptions _rates;
    private readonly IEventBus _eventBus;
    private readonly LootService  _lootService;
    private readonly QuestService _questService;
    private readonly SpawnService _spawnService;
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
    // C3 Phase 1: ai-name → archetype cache (tick thread owned; AiNameRegistry itself is a static, read-only lookup)
    private readonly Dictionary<string, AiArchetype> _archetypeCache  = new(StringComparer.OrdinalIgnoreCase);
    // C3 Phase 1: most-hated retarget throttle — NPC objectId → last re-evaluation time (Java lastChangeTargetTimeDelta, >5s)
    private readonly Dictionary<int, DateTime>    _lastRetargetTime   = new();
    // C3 Phase 2: Trap archetype — NPC objectId → already fired (awaiting despawn). ConcurrentDictionary
    // because the despawn continuation runs on a background Task (fire-and-forget), not the tick thread.
    private readonly ConcurrentDictionary<int, byte> _trapTriggered   = new();
    // M381: summon AI — objectId → last melee-attack timestamp (ATTACK mode cooldown)
    private readonly Dictionary<int, DateTime> _summonLastAttackTime = new();

    public NpcAiService(GameWorld world, PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ExperienceService expService, ILogger<NpcAiService> log, IOptions<RateOptions> rates, IEventBus eventBus,
        LootService lootService, QuestService questService, SpawnService spawnService)
    {
        _world        = world;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
        _expService   = expService;
        _log          = log;
        _rates        = rates.Value;
        _eventBus     = eventBus;
        _lootService  = lootService;
        _questService = questService;
        _spawnService = spawnService;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("NpcAiService started (2-second tick)");
        LogArchetypeDistribution();
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await TickAsync(ct);
        }
    }

    // C3 Phase 1: resolves and memoizes an NPC's behavioral archetype by its template ai-name.
    // Tick-thread owned — safe as a plain Dictionary since only TickAsync (and methods it calls
    // synchronously, like AlertNearbyAllies) touch it.
    private AiArchetype ResolveArchetype(Npc npc)
    {
        string aiName = npc.Template.Ai;
        if (_archetypeCache.TryGetValue(aiName, out var cached)) return cached;
        var archetype = AiNameRegistry.Resolve(aiName);
        _archetypeCache[aiName] = archetype;
        return archetype;
    }

    private void LogArchetypeDistribution()
    {
        var counts = new Dictionary<AiArchetype, int>();
        foreach (var template in _dataManager.Npcs.All)
        {
            var archetype = AiNameRegistry.Resolve(template.Ai);
            counts[archetype] = counts.GetValueOrDefault(archetype) + 1;
        }
        if (counts.Count == 0) return;

        _log.LogInformation(
            "NpcAiService: archetype distribution — Aggressive={Aggressive} General={General} NoAction={NoAction} Interaction={Interaction} Guard={Guard} Trap={Trap}",
            counts.GetValueOrDefault(AiArchetype.Aggressive),
            counts.GetValueOrDefault(AiArchetype.General),
            counts.GetValueOrDefault(AiArchetype.NoAction),
            counts.GetValueOrDefault(AiArchetype.Interaction),
            counts.GetValueOrDefault(AiArchetype.Guard),
            counts.GetValueOrDefault(AiArchetype.Trap));
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
            _lastRetargetTime.Clear();
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
                _lastRetargetTime.Remove(npc.ObjectId);
                _trapTriggered.TryRemove(npc.ObjectId, out _);
                npc.Target = null;
                continue;
            }

            // C3 Phase 1: archetype gates WHETHER this NPC aggro-scans/wanders/fights;
            // NpcAiService below still executes the mechanics identically for every archetype.
            var archetype = ResolveArchetype(npc);

            // C3 Phase 2: Trap runs its own proximity-trigger tick instead of the aggro/combat/
            // wander pipeline below (Java TrapNpcAI2 isMoveSupported=false; only reacts to a
            // creature entering its range, then fires a skill once and despawns).
            if (archetype == AiArchetype.Trap)
            {
                await TickTrapAsync(npc, players, ct);
                continue;
            }

            bool canAggroScan = archetype is AiArchetype.Aggressive or AiArchetype.Guard;
            bool canFight     = archetype is AiArchetype.Aggressive or AiArchetype.General or AiArchetype.Guard;
            bool canWander    = archetype is AiArchetype.Aggressive or AiArchetype.General;
            // Guard holds its post: no random wander, but it still walks an assigned patrol route.
            bool patrolOnly   = archetype == AiArchetype.Guard;

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

            // Aggro + combat — gated to Aggressive/General; NoAction/Interaction never fight (bug #2 fix)
            Player? target = null;
            if (canFight)
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
                        _lastRetargetTime.Remove(npc.ObjectId);
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
                // (only Aggressive archetype scans proactively; General is retaliate-only)
                if (target is null && canAggroScan && npc.Template.AggroRange > 0)
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

                        // Transition to combat stance + face the target (Java Npc.setTarget → SM_LOOKATOBJECT)
                        int engageWorld = npc.Position.WorldId;
                        var attackMode  = new SM_EMOTION(npc, EmotionType.ATTACKMODE);
                        var lookAt      = new SM_LOOKATOBJECT(npc);
                        foreach (var conn in _connRegistry.GetAll())
                            if (conn.ActivePlayer?.Position.WorldId == engageWorld)
                            {
                                try { await conn.SendAsync(attackMode, ct); } catch { }
                                try { await conn.SendAsync(lookAt, ct); } catch { }
                            }

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

            // Wander when idle (Aggressive/General only — NoAction/Interaction never move).
            // Guard never random-wanders off its post, but still walks an assigned patrol route.
            if (target is null)
            {
                if (canWander) await WanderAsync(npc, ct);
                else if (patrolOnly && !string.IsNullOrEmpty(npc.WalkerId)) await WanderAsync(npc, ct);
            }

            if (target is null) continue;

            // C3 Phase 1 bug #1 fix: retarget to the most-hated attacker at most every 5s
            // (Java lastChangeTargetTimeDelta), instead of sticking with whichever player was
            // originally acquired. Leash/return semantics are untouched — this only swaps which
            // in-range player the NPC is chasing/attacking.
            if (npc.HateList.Count > 0)
            {
                var retargetNow = DateTime.UtcNow;
                if (!_lastRetargetTime.TryGetValue(npc.ObjectId, out var lastRetarget)
                    || (retargetNow - lastRetarget).TotalSeconds > 5)
                {
                    _lastRetargetTime[npc.ObjectId] = retargetNow;
                    int topHateId = npc.TopHateObjectId();
                    if (topHateId != 0 && topHateId != target.ObjectId)
                    {
                        var mostHated = players.FirstOrDefault(p => p.ObjectId == topHateId);
                        if (mostHated is not null
                            && !mostHated.IsAlreadyDead
                            && mostHated.Position.WorldId == npc.Position.WorldId
                            && npc.Position.DistanceTo(mostHated.Position) <= ChaseTargetRange)
                        {
                            target = mostHated;
                            _npcTargets[npc.ObjectId] = mostHated.ObjectId;
                            npc.Target = mostHated;
                            _chaseState.Remove(npc.ObjectId); // recompute chase path toward the new target next tick
                        }
                    }
                }
            }

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
            // M259: include target's PdefDebuffDelta + PdefStatUpDelta for parity with CM_CASTSPELL physical defense calc
            int pdef   = target is Player tp ? tp.PhysicalDefense + tp.PdefDebuffDelta + tp.PdefStatUpDelta : 0;
            pdef       = Math.Max(0, pdef);
            int damage = pdef > 0 ? Math.Max(1, rawDmg * 1000 / (1000 + pdef)) : rawDmg;

            // Multi-hit split — Java AttackUtil: NPC hits = Rnd.get(1,3); first = damage*(1-0.1*(n-1)), rest = damage*0.1
            int hitCount = Random.Shared.Next(1, 4);
            var hits = new SM_ATTACK.HitEntry[hitCount];
            hits[0] = new(Math.Max(1, (int)(damage * (1f - 0.1f * (hitCount - 1)))), hitResult);
            int otherHit = hitCount > 1 ? Math.Max(1, (int)(damage * 0.1f)) : 0;
            for (int hi = 1; hi < hitCount; hi++)
                hits[hi] = new(otherHit, hitResult);
            int totalDamage = hits.Sum(h => h.Damage);

            await target.ApplyDamageAndPublishAsync(npc, totalDamage, DamageKind.AutoAttack, skillId: null, _eventBus, ct);

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

        // M381: summon AI — GUARD follows the master, ATTACK chases+melees the recorded target.
        // Kept as its own pass (rather than folded into the NPC loop above) since summons are stored
        // in a dedicated World store, not the NPC store.
        await TickSummonsAsync(ct);
    }

    // M381: one pass per tick over every active summon. GUARD/REST hold position (REST additionally
    // regenerates in Phase 2); ATTACK chases and melees the recorded target, crediting kills to the
    // master via the same loot/quest/xp subset CM_ATTACK uses for player kills.
    private async Task TickSummonsAsync(CancellationToken ct)
    {
        foreach (var summon in _world.GetAllSummons())
        {
            if (summon.Master is null || summon.IsAlreadyDead) continue;

            switch (summon.Mode)
            {
                case SummonMode.Guard:
                    await FollowMasterAsync(summon, ct);
                    break;
                case SummonMode.Attack:
                    await TickSummonAttackAsync(summon, ct);
                    break;
                // Rest/Release: no movement or combat this tick (Phase 2: REST regen)
            }
        }
    }

    // Snap-follow: recomputed every 2s tick, no inter-tick arrival tracking like NPC ChaseAsync —
    // acceptable simplification for a single always-nearby companion (Phase 1 deviation, see report).
    private async Task FollowMasterAsync(Summon summon, CancellationToken ct)
    {
        var master = summon.Master!;
        if (master.Position.WorldId != summon.Position.WorldId) return; // Phase 2: cross-zone/teleport re-follow

        float dist = summon.Position.DistanceTo(master.Position);
        if (dist <= SummonFollowRange) return;

        float dx = master.Position.X - summon.Position.X;
        float dy = master.Position.Y - summon.Position.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.01f) return;

        float stopDist = Math.Max(0, dist - SummonFollowRange * 0.5f);
        float tx = summon.Position.X + dx / len * stopDist;
        float ty = summon.Position.Y + dy / len * stopDist;
        float tz = master.Position.Z;
        byte heading = CalcHeading(dx, dy);

        await BroadcastSummonMoveAsync(summon, tx, ty, tz, heading, ct);
    }

    private async Task TickSummonAttackAsync(Summon summon, CancellationToken ct)
    {
        var master = summon.Master!;
        var target = summon.Target as Npc;
        if (target is null || target.IsAlreadyDead || target.Position.WorldId != summon.Position.WorldId)
        {
            summon.Target = null;
            summon.Mode   = SummonMode.Guard;
            return;
        }

        float dist = summon.Position.DistanceTo(target.Position);
        if (dist > SummonMeleeRange)
        {
            float dx = target.Position.X - summon.Position.X;
            float dy = target.Position.Y - summon.Position.Y;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            float stopDist = Math.Max(0, dist - SummonMeleeRange * 0.8f);
            float tx = summon.Position.X + dx / len * stopDist;
            float ty = summon.Position.Y + dy / len * stopDist;
            float tz = target.Position.Z;
            byte heading = CalcHeading(dx, dy);
            await BroadcastSummonMoveAsync(summon, tx, ty, tz, heading, ct);
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _summonLastAttackTime.GetValueOrDefault(summon.ObjectId)).TotalMilliseconds < SummonAttackCooldownMs) return;
        _summonLastAttackTime[summon.ObjectId] = now;

        // Interim damage formula — summon_stats fidelity is Phase 3 debt; falls back to a level-scaled
        // estimate when the underlying npc_template carries no main_hand_attack (most spirit templates do).
        int baseAtk = summon.Template.Stats?.MainHandAttack ?? Math.Max(1, summon.Level * 5);
        int damage  = Math.Max(1, baseAtk + Random.Shared.Next(-2, 3));

        await target.ApplyDamageAndPublishAsync(summon, damage, DamageKind.AutoAttack, skillId: null, _eventBus, ct);

        int worldId   = summon.Position.WorldId;
        var attackPkt = new SM_ATTACK(summon, target, attackno: 0, time: 0, type: 0, damage);
        var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, 0, damage);
        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try { await conn.SendAsync(attackPkt, ct); } catch { }
            try { await conn.SendAsync(statusPkt, ct); } catch { }
        }

        // Target retaliates against the master, not the summon — Phase 1 avoids lifting the
        // hate/aggro API from Npc onto Creature just for summons (documented pre-decision).
        ForceEngage(target, master);

        if (target.CurrentHp > 0) return;
        await HandleSummonKillAsync(summon, target, master, worldId, ct);
    }

    private async Task BroadcastSummonMoveAsync(Summon summon, float tx, float ty, float tz, byte heading, CancellationToken ct)
    {
        var movePkt = SM_MOVE.StartNpcMove(summon.ObjectId,
            summon.Position.X, summon.Position.Y, summon.Position.Z, heading, tx, ty, tz);
        int worldId = summon.Position.WorldId;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(movePkt, ct); } catch { }

        // Phase 1 simplification: position is snapped to the destination immediately rather than
        // interpolated with arrival tracking like NPC ChaseAsync/WanderAsync (_chaseState/_wanderState).
        // Acceptable for a single always-nearby companion at a 2s tick cadence; revisit if summons ever
        // need believable mid-flight collision/position for other systems.
        summon.Position = summon.Position with { X = tx, Y = ty, Z = tz, Heading = heading };
    }

    // Mirrors the CM_ATTACK.cs Npc-death branch's reward subset (loot/quest/xp), credited to the
    // summon's master rather than the summon itself — summons have no player connection of their own.
    private async Task HandleSummonKillAsync(Summon summon, Npc deadNpc, Player master, int worldId, CancellationToken ct)
    {
        deadNpc.State |= CreatureState.Dead;
        var diePkt = new SM_EMOTION(deadNpc, EmotionType.DIE);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(diePkt, ct); } catch { }

        _world.Remove(deadNpc);
        _npcTargets.TryRemove(deadNpc.ObjectId, out _);

        bool shouldReward = AiNameRegistry.ShouldReward(deadNpc.Template.Ai);
        if (shouldReward)
            _lootService.GenerateDrops(deadNpc, master);

        var masterConn = _connRegistry.Get(master.ObjectId);
        if (masterConn is not null)
            await _questService.HandleNpcKillAsync(master, deadNpc, masterConn, ct);

        if (shouldReward)
        {
            long xpBase = deadNpc.Template.Stats?.MaxXp > 0 ? deadNpc.Template.Stats.MaxXp : deadNpc.Level * 50L;
            await _expService.AddGroupExpAsync(master, xpBase, deadNpc.Level, ct);
        }

        var pendingDrops = _lootService.GetLoot(deadNpc.ObjectId);
        int decayMs = pendingDrops is null ? 5_000 : pendingDrops.Count == 0 ? 90_000 : 300_000;

        var registry = _connRegistry;
        var lootSvc  = _lootService;
        var spawnSvc = _spawnService;
        _ = Task.Run(async () =>
        {
            await Task.Delay(decayMs);
            var del = new SM_DELETE(deadNpc.ObjectId);
            foreach (var c in registry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == worldId)
                    try { await c.SendAsync(del); } catch { }
            lootSvc.ClearLoot(deadNpc.ObjectId);
            spawnSvc.ScheduleRespawn(deadNpc);
        });

        summon.Target = null;
        summon.Mode   = SummonMode.Guard;
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
        await CastSkillEntryAsync(npc, target, entry, now, worldId, ct);
    }

    // C3 Phase 2: shared skill-cast dispatch, factored out of TryCastNpcSkillAsync so the Trap
    // archetype's one-shot trigger (TickTrapAsync) gets the exact same SM_CASTSPELL / effect
    // application / SM_SKILL_ACTIVATION handling as any other NPC skill cast, instead of a parallel
    // implementation.
    private async Task CastSkillEntryAsync(Npc npc, Player target, NpcSkillData.NpcSkillEntry entry,
        DateTime now, int worldId, CancellationToken ct)
    {
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
                await CastNpcHealAsync(npc, entry.SkillId, entry.SkillLevel, skillTemplate, now, worldId, ct);
                break;
            case SkillSubType.BUFF or SkillSubType.CHANT:
                await CastNpcBuffAsync(npc, entry.SkillId, entry.SkillLevel, effectiveDuration, skillTemplate.Effects, now, worldId, ct);
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
                    skillTemplate.Effects?.PhysCritResistAddDelta    ?? 0,
                    skillTemplate.Effects?.MagicCritResistAddDelta   ?? 0,
                    skillTemplate.Effects?.StrikeFortitudeAddDelta   ?? 0,
                    skillTemplate.Effects?.SpellFortitudeAddDelta    ?? 0,
                    skillTemplate.Effects?.CastTimeAddDelta          ?? 0,
                    skillTemplate.Effects?.ConcentrationAddDelta     ?? 0,
                    skillTemplate.Effects?.MagicSuppressionAddDelta  ?? 0,
                    skillTemplate.Effects?.PdefStatUpDelta           ?? 0,
                    skillTemplate.Effects?.MagicDefAddDelta          ?? 0,
                    skillTemplate.DispelCategory,    // M380
                    skillTemplate.ReqDispelLevel,    // M380
                    now, worldId, ct);
                break;
            default:
                await CastNpcDamageAsync(npc, target, entry.SkillId, entry.SkillLevel, skillTemplate, now, worldId, ct);
                break;
        }

        var activationPkt = new SM_SKILL_ACTIVATION(entry.SkillId);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(activationPkt, ct); } catch { }
    }

    private async Task CastNpcDamageAsync(Npc npc, Player target, int skillId, int skillLevel,
        SkillTemplate? skillTemplate, DateTime now, int worldId, CancellationToken ct)
    {
        bool isPhysical = skillTemplate?.SkillType == SkillType.PHYSICAL;

        // Use per-skill damage template when available; fall back to level-scaled estimate
        var dmgFx = skillTemplate?.Effects?.DamageEffects;

        // M346: magic resist check for NPC magical skills (Java calculateMagicalResistRate; accMod=0 for NPC skills)
        bool npcNoResist = dmgFx is { Count: > 0 } && dmgFx[0].IsNoResist;
        if (!isPhysical && !npcNoResist)
        {
            int npcMagAcc  = (int)(npc.Level * (33.6f - 0.16f * npc.Level) + 5f);
            int playerMR   = target.BonusMagicResist + target.MResistDebuffDelta + target.MResistStatUpDelta;
            int npcResistR = Math.Max(1, playerMR - npcMagAcc);
            int lvlDiffNpc = target.Level - npc.Level - 2;
            if (lvlDiffNpc > 0) npcResistR += lvlDiffNpc * 100;
            if (Random.Shared.Next(1000) < npcResistR)
            {
                var resistPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, skillId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == worldId)
                        try { await c.SendAsync(resistPkt, ct); } catch { }
                return;
            }
        }
        int skillBase = dmgFx is { Count: > 0 }
            ? dmgFx[0].BaseValue + dmgFx[0].Delta * (skillLevel - 1)
            : npc.Level * 8 + Random.Shared.Next(10, 40);

        int rawSpellDmg = Math.Max(1, skillBase);
        int defense     = isPhysical
            ? Math.Max(0, target.PhysicalDefense + target.PdefDebuffDelta + target.PdefStatUpDelta)
            : Math.Max(0, target.MagicDefense    + target.MagicDefDelta);
        int spellDmg    = defense > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + defense)) : rawSpellDmg;
        // M253: noreducespellatk — defense-bypass damage replaces regular formula for NPC casters
        var npcNoReduce = skillTemplate?.Effects?.NoReduceEffects;
        if (npcNoReduce is { Count: > 0 })
        {
            int noReduceVal = npcNoReduce[0].BaseValue + npcNoReduce[0].Delta * (skillLevel - 1);
            spellDmg = npcNoReduce[0].IsPercent ? Math.Max(1, target.MaxHp * noReduceVal / 100) : Math.Max(1, noReduceVal);
        }
        // M342: elemental resistance — reduce NPC magical damage if target has resist for this element
        if (!isPhysical && npcNoReduce is not { Count: > 0 })
        {
            string npcElem = dmgFx is { Count: > 0 } ? dmgFx[0].Element : "";
            int npcElemResist = npcElem switch
            {
                "FIRE"  => target.FireResist,
                "WATER" => target.WaterResist,
                "WIND"  => target.WindResist,
                "EARTH" => target.EarthResist,
                _       => 0,
            };
            if (npcElemResist > 0)
                spellDmg = Math.Max(1, (int)(spellDmg * (1f - npcElemResist / 1250f)));
        }

        // M346: crit check for NPC skill casts (physical = 2.0× minus strike fortitude; magical = 1.5×)
        int npcCritPwr = npc.Template.Stats?.Power > 0 ? npc.Template.Stats.Power : 10;
        double npcSkillCritRate = npcCritPwr <= 440 ? npcCritPwr * 0.1
                                : npcCritPwr <= 600 ? 44.0 + (npcCritPwr - 440) * 0.05
                                :                     52.0 + (npcCritPwr - 600) * 0.02;
        if (Random.Shared.Next(100) < (int)npcSkillCritRate)
        {
            if (isPhysical)
            {
                int sFort = target.BonusStrikeFortitude + target.StrikeFortitudeDelta;
                float physCritCoeff = Math.Max(1.0f, 2.0f - (float)Math.Round(sFort / 1000.0));
                spellDmg = (int)(spellDmg * physCritCoeff);
            }
            else
            {
                int spFort = target.BonusSpellFortitude + target.SpellFortitudeDelta;
                float magCritCoeff = Math.Max(1.0f, 1.5f - (float)Math.Round(spFort / 1000.0));
                spellDmg = (int)(spellDmg * magCritCoeff);
            }
        }

        var npcSkillKind = isPhysical ? DamageKind.PhysicalSkill : DamageKind.MagicalSkill;
        await target.ApplyDamageAndPublishAsync(npc, spellDmg, npcSkillKind, skillId, _eventBus, ct);

        // M241: drain damage variants — NPC restores HP/MP from dealt damage
        if (dmgFx is { Count: > 0 } && (dmgFx[0].HpPercent != 0 || dmgFx[0].MpPercent != 0))
        {
            if (dmgFx[0].HpPercent != 0)
                npc.CurrentHp = Math.Min(npc.MaxHp, npc.CurrentHp + spellDmg * dmgFx[0].HpPercent / 100);
            if (dmgFx[0].MpPercent != 0)
                npc.CurrentMp = Math.Min(npc.MaxMp, npc.CurrentMp + spellDmg * dmgFx[0].MpPercent / 100);
        }

        // M241: mpattackinstant — burn target MP
        var npcMpFx = skillTemplate?.Effects?.MpAttackEffects;
        if (npcMpFx is { Count: > 0 })
        {
            int mpBurnVal = npcMpFx[0].BaseValue + npcMpFx[0].Delta * (skillLevel - 1);
            int mpBurn = npcMpFx[0].IsPercent ? target.MaxMp * mpBurnVal / 100 : mpBurnVal;
            if (mpBurn > 0)
                target.CurrentMp = Math.Max(0, target.CurrentMp - mpBurn);
        }

        // M256: NPC skill damage uses LogId.SpellAtk (1) — matches CM_CASTSPELL player-side and Java SpellAtkInstantEffect
        var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, skillId, spellDmg, SM_ATTACK_STATUS.LogId.SpellAtk);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(statusPkt, ct); } catch { }
        await BroadcastGroupHpAsync(target, ct);

        // Apply DoT (bleed/poison/disease) effects from skill template
        if (target.CurrentHp > 0 && skillTemplate?.Effects?.DotEffects is { Count: > 0 } npcDots)
        {
            foreach (var dot in npcDots)
            {
                int rawNpcDot = dot.BaseValue + dot.Delta * skillLevel;
                // M345: elemental resistance for NPC bleed/poison/disease DoT ticks (mirrors M343 player-cast path)
                int npcDotElemResist = dot.Element switch
                {
                    "FIRE"  => target.FireResist,
                    "WATER" => target.WaterResist,
                    "WIND"  => target.WindResist,
                    "EARTH" => target.EarthResist,
                    _       => 0,
                };
                int dotTickDmg = npcDotElemResist > 0
                    ? Math.Max(1, (int)(rawNpcDot * (1f - npcDotElemResist / 1250f)))
                    : Math.Max(1, rawNpcDot);
                var dotExpiry  = DateTime.UtcNow.AddMilliseconds(dot.Duration2Ms);
                var dotEffect  = new AbnormalState
                {
                    SkillId    = skillId,
                    SkillLevel = skillLevel,
                    EffectorId = npc.ObjectId,
                    Expiry     = dotExpiry,
                    DotInfo    = dot,
                    IsDebuff   = true,
                };
                target.AddEffect(dotEffect);
                var dotAbnormal = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer: true, target.GetActiveEffects());
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == worldId)
                        try { await conn.SendAsync(dotAbnormal, ct); } catch { }

                var dotTickTarget = target;
                var dotTickEffect = dotEffect;
                var dotTickCaster = npc;
                var dotTickInfo   = dot;
                var dotTickLogId  = dot.DotType switch
                {
                    "bleed"         => SM_ATTACK_STATUS.LogId.Bleed,
                    "spellatk"      => SM_ATTACK_STATUS.LogId.SpellAtk,
                    "spellatkdrain" => SM_ATTACK_STATUS.LogId.SpellAtkDrain,
                    _               => SM_ATTACK_STATUS.LogId.Poison,
                };
                _ = Task.Run(async () =>
                {
                    while (!dotTickTarget.IsAlreadyDead && DateTime.UtcNow < dotTickEffect.Expiry)
                    {
                        await Task.Delay(dotTickInfo.CheckTimeMs);
                        if (dotTickTarget.IsAlreadyDead || DateTime.UtcNow >= dotTickEffect.Expiry) break;
                        await dotTickTarget.ApplyDamageAndPublishAsync(dotTickCaster, dotTickDmg, DamageKind.DoTTick, skillId, _eventBus);
                        if (dotTickInfo.HpPercent != 0)
                            dotTickCaster.CurrentHp = Math.Min(dotTickCaster.MaxHp, dotTickCaster.CurrentHp + dotTickDmg * dotTickInfo.HpPercent / 100);
                        if (dotTickInfo.MpPercent != 0)
                            dotTickCaster.CurrentMp = Math.Min(dotTickCaster.MaxMp, dotTickCaster.CurrentMp + dotTickDmg * dotTickInfo.MpPercent / 100);
                        var tickPkt = new SM_ATTACK_STATUS(dotTickTarget, SM_ATTACK_STATUS.AttackType.Damage, skillId, dotTickDmg, dotTickLogId);
                        int tw = dotTickTarget.Position.WorldId;
                        foreach (var conn in _connRegistry.GetAll())
                            if (conn.ActivePlayer?.Position.WorldId == tw)
                                try { await conn.SendAsync(tickPkt); } catch { }
                    }
                    dotTickTarget.RemoveEffect(dotTickEffect.SkillId, dotTickEffect.Expiry);
                    var expiredDot = new SM_ABNORMAL_EFFECT(dotTickTarget.ObjectId, isPlayer: true, dotTickTarget.GetActiveEffects());
                    int dw = dotTickTarget.Position.WorldId;
                    foreach (var conn in _connRegistry.GetAll())
                        if (conn.ActivePlayer?.Position.WorldId == dw)
                            try { await conn.SendAsync(expiredDot); } catch { }
                });
            }
        }

        // Caster-centered AoE splash: hit additional players near the NPC
        if (skillTemplate?.IsCasterAoe == true && skillTemplate.EffectiveRange > 0)
        {
            float aoeR    = skillTemplate.EffectiveRange;
            float aoeAlt  = Math.Max(1f, skillTemplate.EffectiveAltitude);
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
                // M347: magic resist check for NPC AoE splash (mirrors M346 primary target)
                if (!isPhysical && !npcNoResist && other is Player splashResistTarget)
                {
                    int npcMagAccSpl  = (int)(npc.Level * (33.6f - 0.16f * npc.Level) + 5f);
                    int splMR         = splashResistTarget.BonusMagicResist + splashResistTarget.MResistDebuffDelta + splashResistTarget.MResistStatUpDelta;
                    int splResistRate = Math.Max(1, splMR - npcMagAccSpl);
                    int splLvlDiff    = splashResistTarget.Level - npc.Level - 2;
                    if (splLvlDiff > 0) splResistRate += splLvlDiff * 100;
                    if (Random.Shared.Next(1000) < splResistRate)
                    {
                        var splResistPkt = new SM_ATTACK_STATUS(other, SM_ATTACK_STATUS.AttackType.Damage, skillId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                        foreach (var c in _connRegistry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == worldId)
                                try { await c.SendAsync(splResistPkt, ct); } catch { }
                        continue;
                    }
                }

                int splashRaw  = Math.Max(1, skillBase + Random.Shared.Next(0, Math.Max(1, skillBase / 10)));
                int splashDef  = isPhysical
                    ? Math.Max(0, other.PhysicalDefense + other.PdefDebuffDelta + other.PdefStatUpDelta)
                    : Math.Max(0, other.MagicDefense    + other.MagicDefDelta);
                int splashDmg  = splashDef > 0 ? Math.Max(1, splashRaw * 1000 / (1000 + splashDef)) : splashRaw;
                // M253: noreducespellatk — defense-bypass damage on NPC splash
                if (npcNoReduce is { Count: > 0 })
                {
                    int splNoReduceVal = npcNoReduce[0].BaseValue + npcNoReduce[0].Delta * (skillLevel - 1);
                    splashDmg = npcNoReduce[0].IsPercent ? Math.Max(1, other.MaxHp * splNoReduceVal / 100) : Math.Max(1, splNoReduceVal);
                }
                // M347: elemental resistance for NPC AoE splash
                if (!isPhysical && npcNoReduce is not { Count: > 0 })
                {
                    string splElem = dmgFx is { Count: > 0 } ? dmgFx[0].Element : "";
                    int splElemResist = splElem switch
                    {
                        "FIRE"  => other.FireResist,
                        "WATER" => other.WaterResist,
                        "WIND"  => other.WindResist,
                        "EARTH" => other.EarthResist,
                        _       => 0,
                    };
                    if (splElemResist > 0)
                        splashDmg = Math.Max(1, (int)(splashDmg * (1f - splElemResist / 1250f)));
                }
                // M347: crit check for NPC AoE splash (same power-stat formula as M346 primary path)
                if (Random.Shared.Next(100) < (int)npcSkillCritRate)
                {
                    if (isPhysical)
                    {
                        int sFortSpl = other is Player pSpl ? pSpl.BonusStrikeFortitude + pSpl.StrikeFortitudeDelta : 0;
                        float physCritCoeffSpl = Math.Max(1.0f, 2.0f - (float)Math.Round(sFortSpl / 1000.0));
                        splashDmg = (int)(splashDmg * physCritCoeffSpl);
                    }
                    else
                    {
                        int spFortSpl = other is Player pSpFort ? pSpFort.BonusSpellFortitude + pSpFort.SpellFortitudeDelta : 0;
                        float magCritCoeffSpl = Math.Max(1.0f, 1.5f - (float)Math.Round(spFortSpl / 1000.0));
                        splashDmg = (int)(splashDmg * magCritCoeffSpl);
                    }
                }
                await other.ApplyDamageAndPublishAsync(npc, splashDmg, DamageKind.Splash, skillId, _eventBus, ct);

                // M241: drain on splash hits — NPC restores HP/MP per splash target
                if (dmgFx is { Count: > 0 } && (dmgFx[0].HpPercent != 0 || dmgFx[0].MpPercent != 0))
                {
                    if (dmgFx[0].HpPercent != 0)
                        npc.CurrentHp = Math.Min(npc.MaxHp, npc.CurrentHp + splashDmg * dmgFx[0].HpPercent / 100);
                    if (dmgFx[0].MpPercent != 0)
                        npc.CurrentMp = Math.Min(npc.MaxMp, npc.CurrentMp + splashDmg * dmgFx[0].MpPercent / 100);
                }
                // M241: mpattackinstant on splash hits
                var splMpFx = skillTemplate?.Effects?.MpAttackEffects;
                if (splMpFx is { Count: > 0 })
                {
                    int splMpBurnVal = splMpFx[0].BaseValue + splMpFx[0].Delta * (skillLevel - 1);
                    int splMpBurn = splMpFx[0].IsPercent ? other.MaxMp * splMpBurnVal / 100 : splMpBurnVal;
                    if (splMpBurn > 0)
                        other.CurrentMp = Math.Max(0, other.CurrentMp - splMpBurn);
                }

                // M256: NPC splash damage uses LogId.SpellAtk
                var splashPkt = new SM_ATTACK_STATUS(other, SM_ATTACK_STATUS.AttackType.Damage, skillId, splashDmg, SM_ATTACK_STATUS.LogId.SpellAtk);
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

    private async Task CastNpcHealAsync(Npc npc, int skillId, int skillLevel, SkillTemplate? skillTemplate, DateTime now, int worldId, CancellationToken ct)
    {
        // M257: NPC heal skills use template HealEffects (HP only — NPCs don't have MP/FP/DP)
        // Fallback for skills without parsed HealEffects: MaxHp/6 (legacy behaviour)
        int healAmt;
        var healFx = skillTemplate?.Effects?.HealEffects;
        if (healFx is { Count: > 0 })
        {
            healAmt = 0;
            foreach (var he in healFx)
            {
                if (he.HealType != "hp") continue; // NPC heal targets only HP
                int valueWithDelta = he.BaseValue + he.Delta * skillLevel;
                int part = he.IsPercent ? npc.MaxHp * valueWithDelta / 100 : valueWithDelta;
                if (part > 0) healAmt += part;
            }
            if (healAmt <= 0) healAmt = Math.Max(1, npc.MaxHp / 6); // safety fallback
        }
        else
        {
            healAmt = Math.Max(1, npc.MaxHp / 6);
        }
        healAmt = Math.Min(healAmt, npc.MaxHp - npc.CurrentHp);
        if (healAmt <= 0) return;

        npc.CurrentHp      = npc.CurrentHp + healAmt;
        npc.LastCombatTime = now;

        // M256: NPC self-heal uses LogId.Heal (3)
        var statusPkt = new SM_ATTACK_STATUS(npc, SM_ATTACK_STATUS.AttackType.NaturalHp, skillId, healAmt, SM_ATTACK_STATUS.LogId.Heal);
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(statusPkt, ct); } catch { }
    }

    private async Task CastNpcBuffAsync(Npc npc, int skillId, int skillLevel, int durationMs,
        SkillEffects? effects, DateTime now, int worldId, CancellationToken ct)
    {
        if (durationMs <= 0) return;
        npc.LastCombatTime = now;

        int speedStatUpPct = effects?.SpeedStatUpPct ?? 0;
        var effect = new AbnormalState
        {
            SkillId = skillId, SkillLevel = skillLevel,
            EffectorId = npc.ObjectId, Expiry = DateTime.UtcNow.AddMilliseconds(durationMs),
            MaxHpDelta               = effects?.MaxHpStatUpDelta            ?? 0,
            MaxMpDelta               = effects?.MaxMpStatUpDelta            ?? 0,
            MagicBoostDeltaVal       = effects?.MagicBoostStatUpDelta       ?? 0,
            HealBoostDeltaVal        = effects?.HealBoostStatUpDelta        ?? 0,
            PhysAccDeltaVal          = effects?.PhysAccStatUpDelta          ?? 0,
            MagicAccDeltaVal         = effects?.MagicAccStatUpDelta         ?? 0,
            ParryDeltaVal            = effects?.ParryStatUpDelta            ?? 0,
            BlockDeltaVal            = effects?.BlockStatUpDelta            ?? 0,
            PhysCritDeltaVal         = effects?.PhysCritStatUpDelta         ?? 0,
            MagicCritDeltaVal        = effects?.MagicCritStatUpDelta        ?? 0,
            PhysCritResistDeltaVal   = effects?.PhysCritResistStatUpDelta   ?? 0,
            MagicCritResistDeltaVal  = effects?.MagicCritResistStatUpDelta  ?? 0,
            StrikeFortitudeDeltaVal  = effects?.StrikeFortitudeStatUpDelta  ?? 0,
            SpellFortitudeDeltaVal   = effects?.SpellFortitudeStatUpDelta   ?? 0,
            CastTimeDeltaVal         = effects?.CastTimeStatUpDelta         ?? 0,
            ConcentrationDeltaVal    = effects?.ConcentrationStatUpDelta    ?? 0,
            MagicSuppressionDeltaVal = effects?.MagicSuppressionStatUpDelta ?? 0,
            PdefStatUpDeltaVal       = effects?.PdefStatUpDelta             ?? 0,
            MagicDefDeltaVal         = effects?.MagicDefStatUpDelta         ?? 0,
            PatkStatUpDeltaVal       = effects?.PhysAtkStatUpDelta          ?? 0,
            MagicAtkStatUpDeltaVal   = effects?.MagicAtkStatUpDelta         ?? 0,
            EvasionStatUpDeltaVal    = effects?.EvasionStatUpDelta          ?? 0,
            MResistStatUpDeltaVal    = effects?.MResistStatUpDelta          ?? 0,
            AtkSpeedStatUpDeltaVal   = effects?.AtkSpeedStatUpDelta         ?? 0,
            SpeedStatUpPct           = speedStatUpPct,
            PreBuffMovSpeed          = npc.MovementSpeed,
        };
        npc.AddEffect(effect);

        if (speedStatUpPct != 0)
            npc.MovementSpeed = npc.MovementSpeed * (100 + speedStatUpPct) / 100f;

        var abnormal = new SM_ABNORMAL_EFFECT(npc.ObjectId, isPlayer: false, npc.GetActiveEffects());
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(abnormal, ct); } catch { }

        var expEffect = effect;
        _ = Task.Run(async () =>
        {
            await Task.Delay(durationMs);
            npc.RemoveEffectBySkillId(expEffect.SkillId);
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
        int castTimeDelta,
        int concentrationDelta,
        int magicSuppressionDelta,
        int pdefStatUpDelta,
        int magicDefDelta,
        string dispelCategory,    // M380
        int    reqDispelLevel,    // M380
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
            StrikeFortitudeDeltaVal = strikeFortitudeDelta, SpellFortitudeDeltaVal = spellFortitudeDelta,
            CastTimeDeltaVal = castTimeDelta,
            ConcentrationDeltaVal = concentrationDelta, MagicSuppressionDeltaVal = magicSuppressionDelta,
            PdefStatUpDeltaVal = pdefStatUpDelta, MagicDefDeltaVal = magicDefDelta,
            DispelCategory = dispelCategory, ReqDispelLevel = reqDispelLevel };
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

        if (atkSpdDelta != 0)
        {
            var atkSpdEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == worldId)
                    try { await conn.SendAsync(atkSpdEmo, ct); } catch { }
        }
        if (pdefDelta != 0 || mresistDelta != 0 || patkDelta != 0 || evasionDelta != 0 || maxHpDelta != 0 || magicAtkDelta != 0 || atkSpdDelta != 0 || maxMpDelta != 0 || mBoostDebuffDelta != 0 || physAccDelta != 0 || magicAccDelta != 0 || parryDelta != 0 || blockDelta != 0 || physCritDelta != 0 || magicCritDelta != 0 || physCritResistDelta != 0 || magicCritResistDelta != 0 || strikeFortitudeDelta != 0 || spellFortitudeDelta != 0 || castTimeDelta != 0 || concentrationDelta != 0 || magicSuppressionDelta != 0 || pdefStatUpDelta != 0 || magicDefDelta != 0)
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
            bool statChanged    = expEffect.PdefDelta != 0 || expEffect.MResistDelta != 0 ||
                expEffect.PatkDelta != 0 || expEffect.EvasionDelta != 0 ||
                expEffect.MaxHpDelta != 0 || expEffect.MagicAtkDelta != 0 ||
                expEffect.AtkSpeedDelta != 0 || expEffect.MaxMpDelta != 0 ||
                expEffect.MagicBoostDeltaVal != 0 || expEffect.PhysAccDeltaVal != 0 ||
                expEffect.MagicAccDeltaVal != 0 || expEffect.ParryDeltaVal != 0 ||
                expEffect.BlockDeltaVal != 0 || expEffect.PhysCritDeltaVal != 0 ||
                expEffect.MagicCritDeltaVal != 0 || expEffect.PhysCritResistDeltaVal != 0 ||
                expEffect.MagicCritResistDeltaVal != 0 || expEffect.StrikeFortitudeDeltaVal != 0 ||
                expEffect.SpellFortitudeDeltaVal != 0 || expEffect.CastTimeDeltaVal != 0 ||
                expEffect.ConcentrationDeltaVal != 0 || expEffect.MagicSuppressionDeltaVal != 0 ||
                expEffect.PdefStatUpDeltaVal != 0 || expEffect.MagicDefDeltaVal != 0;
            bool speedRestored    = expEffect.MovSpeedPct != 0 || expEffect.AttackSpeedPct != 0;
            bool atkSpeedRestored = expEffect.AtkSpeedDelta != 0;

            target.RemoveEffectBySkillId(expEffect.SkillId);

            if (speedRestored)
            {
                var restoreEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
                int restoreWorld = target.Position.WorldId;
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == restoreWorld)
                        try { await conn.SendAsync(restoreEmo); } catch { }
            }
            if (atkSpeedRestored)
            {
                var restoreAtkEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
                int restoreAtkWorld = target.Position.WorldId;
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == restoreAtkWorld)
                        try { await conn.SendAsync(restoreAtkEmo); } catch { }
            }
            if (statChanged)
            {
                var statsInfo = new SM_STATS_INFO(target, _dataManager.PlayerStats.GetTemplate(target.PlayerClass, target.Level));
                var dc = _connRegistry.GetAll().FirstOrDefault(c => c.ActivePlayer == target);
                if (dc is not null) try { await dc.SendAsync(statsInfo); } catch { }
            }
            var expired = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer: true, target.GetActiveEffects());
            int expWorldId = target.Position.WorldId;
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == expWorldId)
                    try { await conn.SendAsync(expired); } catch { }
        });
    }

    // C3 Phase 2: Trap archetype tick (Java TrapNpcAI2.tryActivateTrap). Traps don't aggro-scan,
    // wander, or chase — they scan for the first enemy player within (aggro range + 2) each tick
    // and, once triggered, cast a skill and despawn. No leash/chase/retaliation semantics apply.
    private async Task TickTrapAsync(Npc npc, List<Player> players, CancellationToken ct)
    {
        if (_trapTriggered.ContainsKey(npc.ObjectId)) return; // already fired, awaiting despawn

        float triggerRange = npc.Template.AggroRange + 2f; // Java: isInRange(creature, getOwner().getAggroRange() + 2)
        Player? victim = null;
        foreach (var player in players)
        {
            if (player.IsAlreadyDead || player.IsHidden) continue;
            if (player.Position.WorldId != npc.Position.WorldId) continue;
            if (!_dataManager.Tribes.IsAggressiveToPlayer(npc.Template.Tribe, player.Race)) continue;
            if (npc.Position.DistanceTo(player.Position) > triggerRange) continue;
            victim = player;
            break;
        }
        if (victim is null) return;

        if (!_trapTriggered.TryAdd(npc.ObjectId, 0)) return;
        npc.Target = victim;

        var skills = _dataManager.NpcSkills.GetSkills(npc.Template.NpcId);
        if (skills is { Count: > 0 })
        {
            var entry = skills[Random.Shared.Next(skills.Count)];
            await CastSkillEntryAsync(npc, victim, entry, DateTime.UtcNow, npc.Position.WorldId, ct);
        }

        // Java TrapDelete: AI2Actions.deleteOwner after a short delay — no loot/XP/respawn
        // (SHOULD_REWARD/SHOULD_RESPAWN/SHOULD_DECAY are all NEGATIVE for traps).
        const int TrapDespawnDelayMs = 1000;
        var despawnNpc   = npc;
        var despawnWorld = npc.Position.WorldId;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TrapDespawnDelayMs);
            _world.Remove(despawnNpc);
            _trapTriggered.TryRemove(despawnNpc.ObjectId, out _);
            var del = new SM_DELETE(despawnNpc.ObjectId);
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer?.Position.WorldId == despawnWorld)
                    try { await conn.SendAsync(del); } catch { }
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
        if (player.IsHidden) return; // M301: hidden players do not trigger NPC aggro
        // C3 Phase 1/2: NoAction/Interaction never fight back; Trap only triggers via its own
        // proximity scan (TickTrapAsync), never via melee retaliation.
        // Uses the static registry directly (not the tick-thread-owned _archetypeCache) since
        // ForceEngage is invoked from packet-handler threads, not the tick thread.
        var archetype = AiNameRegistry.Resolve(npc.Template.Ai);
        if (archetype is AiArchetype.NoAction or AiArchetype.Interaction or AiArchetype.Trap) return;
        npc.LastCombatTime = DateTime.UtcNow; // refresh on every hit so idle timeout resets
        // M333: scale baseline hate by BOOST_HATE (from active buffs + passive Aggravation skills)
        int boostHatePct = player.BoostHatePct + PassiveBoostHateHelper.ComputePct(player, _dataManager);
        int hate = boostHatePct != 0 ? Math.Max(1, 1 * (100 + boostHatePct) / 100) : 1;
        npc.AddHate(player.ObjectId, hate);
        if (_npcTargets.TryAdd(npc.ObjectId, player.ObjectId))
        {
            npc.Target = player;

            // Broadcast combat stance + face the target — fire-and-forget since ForceEngage is sync
            int engageWorld = npc.Position.WorldId;
            var attackMode  = new SM_EMOTION(npc, EmotionType.ATTACKMODE);
            var lookAt      = new SM_LOOKATOBJECT(npc);
            _ = Task.Run(async () =>
            {
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer?.Position.WorldId == engageWorld)
                    {
                        try { await conn.SendAsync(attackMode); } catch { }
                        try { await conn.SendAsync(lookAt); } catch { }
                    }
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
            // ally-assist is proactive aggro — Guard scans/assists like Aggressive, everything else doesn't
            if (ResolveArchetype(ally) is not (AiArchetype.Aggressive or AiArchetype.Guard)) continue;
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
