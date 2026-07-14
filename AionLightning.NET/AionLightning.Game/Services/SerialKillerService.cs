using AionLightning.Commons.Services;
using AionLightning.Game.Combat.Effects;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>Java services.SerialKillerService's WorldType — the home race a handled world is registered to.</summary>
public enum SerialKillerWorldType { ASMODIANS, ELYOS, USEALL }

/// <summary>
/// Port of Java services.SerialKillerService + services.serialkillers.{SerialKiller,SerialKillerDebuff}
/// + data/static_data/serial_killer/serial_killer.xml.
///
/// IMPORTANT — this is NOT a "kill your own race" system, despite the feature's common name. Java's
/// <c>PvpService.doReward</c> returns immediately when <c>winner.getRace() == victim.getRace()</c>
/// (before <c>SerialKillerService.updateRank</c> is ever reached), so this only ever fires for
/// cross-race PvP kills — verified by reading the actual 4.6 source, not assumed. The mechanic is: a
/// player who kills enemy-race players while personally standing inside *that enemy race's home
/// territory* (Java "enemy world" — see <see cref="IsEnemyWorld"/>) racks up a victim counter; past two
/// thresholds they become a rank-1/2 "serial killer" carrying a stat-penalty debuff, visible to
/// themselves via <see cref="SM_SERIAL_KILLER"/>. It is effectively a griefer/invader penalty (only
/// counts kills against victims at least <see cref="SerialKillerOptions.LevelDiff"/> levels below the
/// killer), not a same-faction-PK tracker.
///
/// Known Java behaviors NOT reproduced here (confirmed by reading the source, not gaps from missing
/// context):
/// - Java's <c>onKillSerialKiller(killer, victim)</c> (the reward-buff-on-SK-kill logic) is dead code —
///   grepping the entire AL-Game source turns up zero callers. This port wires it into the PvP-kill
///   path anyway (see <see cref="OnSerialKillerDeathAsync"/>) because the task this was built against
///   explicitly asked for "killing an SK grants a reward", and the method is fully implemented and
///   clearly intended to run — just never connected. Treat this as completing an orphaned Java feature,
///   not literal parity.
/// - That same orphaned method never resets the victim's rank/victim count on death (it only grants the
///   observer buff). This port does reset them (see doc on <see cref="OnSerialKillerDeathAsync"/>) since
///   "defeating the outlaw ends the rampage" is the only sensible reading once the hook is actually wired.
/// - Java has no item-drop-on-death for serial killers at all (there is no such logic anywhere in
///   SerialKillerService.java) — none is added here either.
/// - The type-4 SM_SERIAL_KILLER "nearby serial killers" map-icon broadcast (onEnterMap/onLeaveMap/
///   updateIcons) and the direct-portal/dynamic-bindstone rank restrictions (<see cref="IsRestrictPortal"/>/
///   <see cref="IsRestrictDynamicBindstone"/>) are exposed for future TeleportService/Kisk wiring but are
///   not called from anywhere yet — that wiring needs a per-world-instance visitor registry this task
///   didn't build.
/// - Java decays a tracked SK's victim count for as long as its map entry exists, including while the
///   owning player is offline (its FastMap holds a raw Player reference keyed by objectId that outlives
///   logout). This port only decays currently-online players (sweeps <see cref="PlayerConnectionRegistry"/>
///   each tick) — reasonable given the whole system is in-memory/non-persisted already (a server
///   restart wipes everyone's rank regardless).
/// </summary>
public sealed class SerialKillerService(
    IDataManager dataManager,
    PlayerConnectionRegistry connRegistry,
    CronService cronService,
    IOptions<SerialKillerOptions> options,
    ILogger<SerialKillerService> log)
{
    // Java SerialKillerDebuff + data/static_data/serial_killer/serial_killer.xml, ported verbatim.
    private readonly record struct RankRestriction(
        int PhysAccDelta, int MagicAccDelta, int PvpAtkRatioDelta, int PvpDefRatioDelta, int SpeedPct,
        bool RestrictDirectPortal, bool RestrictDynamicBindstone);

    private static readonly Dictionary<int, RankRestriction> RankRestrictions = new()
    {
        [1] = new(PhysAccDelta: -530, MagicAccDelta: -460, PvpAtkRatioDelta: -130, PvpDefRatioDelta: -125,
                  SpeedPct: -50, RestrictDirectPortal: true, RestrictDynamicBindstone: false),
        [2] = new(PhysAccDelta: -1100, MagicAccDelta: -800, PvpAtkRatioDelta: -500, PvpDefRatioDelta: -350,
                  SpeedPct: -70, RestrictDirectPortal: true, RestrictDynamicBindstone: true),
    };

    // Synthetic bookkeeping skill ids for the rank debuff. Java's SerialKillerDebuff is a raw
    // StatOwner/IStatFunction modifier with no associated skill id or client buff-bar icon; this port
    // reuses Creature.AddEffect/RemoveEffectBySkillId (the same "apply an effect outside of a skill
    // cast" idiom SiegeService.ApplyBuffSkill/PlayerEnterWorldService's soul-sickness reapply use)
    // since there is no standalone stat-only effect API. Caveat: because AddEffect keys off SkillId,
    // this may surface an unrecognized icon slot client-side — a cosmetic gap only, the stat deltas
    // themselves (accuracy/pvp-ratio/speed) apply correctly regardless via Creature.ApplyEffectDeltas.
    private const int Rank1DebuffSkillId = 990001;
    private const int Rank2DebuffSkillId = 990002;

    // Java buffId(Player, SerialKiller) — reward buff granted to nearby same-race-as-killer players
    // when an active serial killer is killed. Both skill ids are real, existing skill_templates.xml
    // entries (Power of Revenge / Power of Elimination: +200 PVP_ATTACK_RATIO, +400 PVP_DEFEND_RATIO,
    // 600s), so this reuses the normal stat-delta pipeline instead of a synthetic id.
    private const int EmpireRewardBuffSkillId = 8610;
    private const int AbyssRewardBuffSkillId  = 8611;

    // Java MathUtil.isIn3dRange(victim, player, 30) in onKillSerialKiller.
    private const float RewardRangeMeters = 30f;

    private readonly Dictionary<int, SerialKillerWorldType> _handledWorlds = ParseHandledWorlds(options.Value.HandledWorlds);

    /// <summary>Java initSerialKillers() cron half — arms the periodic victim-count decay sweep. No-op
    /// (and no cron armed) while <see cref="SerialKillerOptions.Enabled"/> is false.</summary>
    public async Task ScheduleDecayAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enabled)
        {
            log.LogInformation("SerialKillerService: disabled (GameServer:SerialKiller:Enabled=false) — decay cron not armed.");
            return;
        }

        int minutes = Math.Max(1, options.Value.RefreshMinutes);
        await cronService.Schedule(
            () => FireAndForget(DecayTickAsync(), "SerialKiller decay tick"),
            $"0 0/{minutes} * * * ?", longRunningTask: true);
    }

    /// <summary>
    /// Java SerialKillerService.updateRank(winner, victim), called from PvpService.doReward after the
    /// AP/reward distribution — by the time control reaches that call, winner/victim are already
    /// guaranteed cross-race (doReward returns early on a same-race kill). Only counts toward rank when
    /// the killer is standing in the victim's race's home territory (an "invader") and is at least
    /// <see cref="SerialKillerOptions.LevelDiff"/> levels above the victim.
    /// </summary>
    public async ValueTask OnPvpKillAsync(Player killer, Player victim, CancellationToken ct = default)
    {
        if (!options.Value.Enabled) return;
        if (!IsEnemyWorld(killer)) return;
        if (killer.Level < victim.Level + options.Value.LevelDiff) return;

        killer.SerialKillerVictims++;
        int newRank = GetKillerRank(killer.SerialKillerVictims);
        if (newRank == killer.SerialKillerRank) return;

        killer.SerialKillerRank = newRank;
        ApplyRankDebuff(killer, newRank);
        await SendRankUpdateAsync(killer, ct);
    }

    /// <summary>
    /// Java SerialKillerService.onKillSerialKiller(killer, victim) — see class doc for why this is
    /// wired up here even though Java itself never calls it. Rewards nearby (30m, same world/instance
    /// scope) players who share the killer's race with a PVP ratio buff, then clears the victim's rank
    /// and debuff (not part of Java's orphaned method body — added because the hook is now live and
    /// this is the only sensible behavior for "you killed the outlaw").
    /// </summary>
    public async ValueTask OnSerialKillerDeathAsync(Player killer, Player victim, CancellationToken ct = default)
    {
        if (!options.Value.Enabled) return;
        if (victim.SerialKillerRank <= 0) return;
        if (!IsEnemyWorld(victim)) return;

        int rewardSkillId = killer.Race == Race.ELYOS ? EmpireRewardBuffSkillId : AbyssRewardBuffSkillId;
        foreach (var conn in connRegistry.GetAll())
        {
            var observer = conn.ActivePlayer;
            if (observer is null) continue;
            if (observer.Race != killer.Race) continue;
            if (!observer.Position.SameScope(victim.Position)) continue;
            if (observer.Position.DistanceTo(victim.Position) > RewardRangeMeters) continue;

            ApplyRewardBuff(observer, rewardSkillId);
        }

        victim.SerialKillerRank    = 0;
        victim.SerialKillerVictims = 0;
        RemoveRankDebuff(victim);
        try { await SendRankUpdateAsync(victim, ct); } catch { /* best effort */ }
    }

    /// <summary>Java isRestrictPortal(Player) — not wired to TeleportService yet (see class doc); exposed for future use.</summary>
    public bool IsRestrictPortal(Player player) =>
        player.SerialKillerRank > 0 && RankRestrictions.TryGetValue(player.SerialKillerRank, out var r) && r.RestrictDirectPortal;

    /// <summary>Java isRestrictDynamicBindstone(Player) — not wired to Kisk yet (see class doc); exposed for future use.</summary>
    public bool IsRestrictDynamicBindstone(Player player) =>
        player.SerialKillerRank > 0 && RankRestrictions.TryGetValue(player.SerialKillerRank, out var r) && r.RestrictDynamicBindstone;

    /// <summary>Java isHandledWorld(int).</summary>
    public bool IsHandledWorld(int worldId) => _handledWorlds.ContainsKey(worldId);

    /// <summary>
    /// Java isEnemyWorld(Player) — true when the player is currently inside a handled world registered
    /// to a race other than their own (a USEALL-registered world is always "enemy" for both races).
    /// </summary>
    public bool IsEnemyWorld(Player player)
    {
        if (!_handledWorlds.TryGetValue(player.Position.WorldId, out var type)) return false;
        var homeType = player.Race == Race.ASMODIANS ? SerialKillerWorldType.ASMODIANS : SerialKillerWorldType.ELYOS;
        return type != homeType;
    }

    /// <summary>Java getKillerRank(int kills).</summary>
    private int GetKillerRank(int victims) =>
        victims > options.Value.Rank2Kills ? 2 : victims > options.Value.Rank1Kills ? 1 : 0;

    /// <summary>
    /// Java's ThreadPoolManager.scheduleAtFixedRate decay loop — for every online player with a
    /// positive victim count who is NOT currently in enemy territory, subtracts the configured decay
    /// amount, recomputes rank, and reapplies/clears the debuff + notifies on a rank change.
    /// </summary>
    private async Task DecayTickAsync()
    {
        foreach (var conn in connRegistry.GetAll())
        {
            var player = conn.ActivePlayer;
            if (player is null || player.SerialKillerVictims <= 0) continue;
            if (IsEnemyWorld(player)) continue;

            player.SerialKillerVictims = Math.Max(0, player.SerialKillerVictims - options.Value.DecayPerTick);
            int newRank = GetKillerRank(player.SerialKillerVictims);
            if (newRank == player.SerialKillerRank) continue;

            player.SerialKillerRank = newRank;
            ApplyRankDebuff(player, newRank);
            try { await SendRankUpdateAsync(player, CancellationToken.None); } catch { /* best effort */ }
        }
    }

    private async ValueTask SendRankUpdateAsync(Player player, CancellationToken ct)
    {
        var conn = connRegistry.Get(player.ObjectId);
        if (conn is null) return;
        try { await conn.SendAsync(new SM_SERIAL_KILLER(showMessage: true, player.SerialKillerRank), ct); }
        catch { /* best effort — a dropped connection shouldn't fault the caller */ }
    }

    /// <summary>
    /// Java SerialKillerDebuff.applyEffect/endEffect — always fully replaces the previous rank's
    /// deltas before applying the new rank's (Java: "if (hasDebuff()) endEffect(player);").
    /// </summary>
    private static void ApplyRankDebuff(Player player, int rank)
    {
        RemoveRankDebuff(player);
        if (rank <= 0) return;
        if (!RankRestrictions.TryGetValue(rank, out var restriction)) return;

        var state = new AbnormalState
        {
            SkillId          = rank == 1 ? Rank1DebuffSkillId : Rank2DebuffSkillId,
            EffectorId       = player.ObjectId,
            Expiry           = DateTime.MaxValue,
            IsDebuff         = true,
            PhysAccDeltaVal  = restriction.PhysAccDelta,
            MagicAccDeltaVal = restriction.MagicAccDelta,
            PvpAtkRatioDelta = restriction.PvpAtkRatioDelta,
            PvpDefRatioDelta = restriction.PvpDefRatioDelta,
            MovSpeedPct      = restriction.SpeedPct,
            PreDebuffSpeed   = player.MovementSpeed,
        };
        player.AddEffect(state);

        // Mirrors CM_CASTSPELL's snare/statdown-speed application: AddEffect only accumulates the delta
        // bookkeeping; the actual MovementSpeed mutation (and its restore via ReverseEffectDeltas on
        // removal) happens here, same as every other MovSpeedPct debuff in this codebase.
        if (restriction.SpeedPct != 0)
            player.MovementSpeed = MathF.Max(1.0f, player.MovementSpeed * (100 + restriction.SpeedPct) / 100f);
    }

    private static void RemoveRankDebuff(Player player)
    {
        player.RemoveEffectBySkillId(Rank1DebuffSkillId);
        player.RemoveEffectBySkillId(Rank2DebuffSkillId);
    }

    /// <summary>
    /// Java SkillEngine.getInstance().applyEffectDirectly(buffId, player, player, 0) — this port has no
    /// standalone "apply a skill's buff outside of a live cast" service (see SiegeService.ApplyBuffSkill's
    /// doc for the same note), so it reuses the pure stat-delta calculator CM_CASTSPELL/SiegeService rely
    /// on for the PVP_ATTACK_RATIO/PVP_DEFEND_RATIO subset both reward skills (8610/8611) carry.
    /// </summary>
    private void ApplyRewardBuff(Player player, int skillId)
    {
        var template = dataManager.Skills.GetTemplate(skillId);
        if (template is null) return;

        var stat = StatEffectCalculator.Compute(template, player, level: 0);
        int durationMs = template.Duration > 0 ? template.Duration : template.Effects?.EffectDuration ?? 0;

        player.AddEffect(new AbnormalState
        {
            SkillId          = skillId,
            EffectorId       = player.ObjectId,
            Expiry           = durationMs > 0 ? DateTime.UtcNow.AddMilliseconds(durationMs) : DateTime.MaxValue,
            PvpAtkRatioDelta = stat.PvpAtkRatioDelta,
            PvpDefRatioDelta = stat.PvpDefRatioDelta,
        });
    }

    /// <summary>
    /// Java initSerialKillers()'s world-registry parse: <c>world.charAt(1)</c> (the world id's 2nd
    /// digit) encodes its home race. Ported including the original's quirky early-exit on an empty
    /// token (Java: <c>if ("".equals(world)) break;</c> — not <c>continue</c>).
    /// </summary>
    private static Dictionary<int, SerialKillerWorldType> ParseHandledWorlds(string config)
    {
        var result = new Dictionary<int, SerialKillerWorldType>();
        if (string.IsNullOrEmpty(config)) return result;

        foreach (var token in config.Split(','))
        {
            if (token.Length == 0) break;
            if (!int.TryParse(token, out int worldId) || token.Length < 2) continue;

            int worldType = token[1] - '0';
            var type = worldType > 0
                ? (worldType > 1 ? SerialKillerWorldType.ASMODIANS : SerialKillerWorldType.ELYOS)
                : SerialKillerWorldType.USEALL;
            result[worldId] = type;
        }

        return result;
    }

    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "Unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);
}
