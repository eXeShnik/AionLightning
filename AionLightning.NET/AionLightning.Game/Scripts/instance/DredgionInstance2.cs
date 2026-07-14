// Port of Java data/scripts/system/handlers/instance/dredgion2/{DredgionInstance2,
// BaranathDredgionInstance2, ChantraDredgionInstance2, TerathDredgionInstance2}.java.
//
// Scope (see task brief): this ports the *scoring/reward framework and completion path* faithfully
// — per-race point totals, the win/loss AP payout, and firing the dredgion-reward quest hook for
// every player still inside when the run ends — which is what unblocks the already-migrated
// chantra_dredgion (4725/3725) and reshanta (4718/3718) quests. It deliberately does NOT port:
//   - The three maps' per-NPC mob spawn timelines / boss chains (sp/spawn calls with exact
//     coordinates and walker ids) and the resulting door-progression subplot (escape-hatch /
//     captain's-cabin teleporter NPCs). Those are content data, not scoring logic, and porting them
//     needs the full instanced-spawn table for each ship — tracked separately.
//   - Per-mob-id room capture / point awards on NPC kill (Java onDie(Npc) + onDieSurkan): those
//     depend on Creature.AggroList.getMostPlayerDamage(), which isn't ported yet (same gap already
//     noted in PvpKillHandler/SiegeBossDamageHandler — "Phase 2, needs AggroList on Creature").
//     Mob kills inside a dredgion currently award no score; the run still completes and rewards
//     players via the 42-minute auto-finish timer below (Java's own fallback path when nobody
//     kills the finishing boss).
//   - Custom onReviveEvent (teleport-to-race-start-position + custom revive) — default revive
//     handling applies instead.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Instance;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Instance;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using AionLightning.Game.World;

namespace Instance;

public abstract class DredgionInstance2 : GeneralInstanceHandler
{
    // note: Java RateConfig.DREDGION_REWARD_RATE has no ported config entry (RateOptions doesn't
    // carry a dredgion-specific multiplier yet) — treated as 1.0 until that config lands.
    private const float DredgionRewardRate = 1.0f;
    private const int BaseDeathPenalty = 60;

    protected DredgionReward Reward { get; private set; } = null!;

    private float _loosingGroupMultiplier = 1f;
    private bool _isInstanceDestroyed;
    private int _startedFlag;
    private long _instanceStartUtcMs;
    private CancellationTokenSource? _autoFinishCts;

    /// <summary>Java <c>AtomicBoolean isInstanceStarted.compareAndSet(false, true)</c>.</summary>
    protected bool TryStartOnce() => Interlocked.CompareExchange(ref _startedFlag, 1, 0) == 0;

    public override void OnInstanceCreate(WorldMapInstance instance)
    {
        base.OnInstanceCreate(instance);
        Reward = new DredgionReward(WorldId, InstanceId) { ScoreType = InstanceScoreType.PREPARING };
    }

    public override void OnEnterInstance(Player player)
    {
        if (!Reward.ContainsPlayer(player.ObjectId))
            Reward.AddPlayerReward(new DredgionPlayerReward(player.ObjectId));
        SendScorePacket();
    }

    public override void OnInstanceDestroy()
    {
        _autoFinishCts?.Cancel();
        _isInstanceDestroyed = true;
        Reward.Clear();
    }

    /// <summary>Java startInstanceTask(): opens the entry doors and flips the scoreboard to
    /// START_PROGRESS after 2 minutes; auto-completes the run 42 minutes after start if nothing
    /// else (a finishing-boss kill, in the unported per-map subclasses) ends it first.</summary>
    protected void StartInstanceTask()
    {
        _instanceStartUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _autoFinishCts = new CancellationTokenSource();
        var autoFinishToken = _autoFinishCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            if (_isInstanceDestroyed) return;
            OpenFirstDoors();
            Reward.ScoreType = InstanceScoreType.START_PROGRESS;
            SendScorePacket();
        });

        _ = Task.Run(async () =>
        {
            try { await Task.Delay(TimeSpan.FromMinutes(42), autoFinishToken); }
            catch (OperationCanceledException) { return; }
            await StopInstanceAsync(Reward.WinningRaceByScore);
        });
    }

    /// <summary>Java's per-map door-open list (subclasses override with their own static ids).</summary>
    protected virtual void OpenFirstDoors() { }

    protected void OpenDoor(int staticId) => SetDoorState(staticId, true);

    private async ValueTask StopInstanceAsync(Race winner)
    {
        _autoFinishCts?.Cancel();
        Reward.WinningRace = winner;
        Reward.ScoreType = InstanceScoreType.END_PROGRESS;
        await DoRewardAsync();
        SendScorePacket();
    }

    /// <summary>
    /// Java doReward(): grants AP and fires the dredgion-reward quest hook for every player still
    /// inside, then evicts the channel to the instance exit 10 seconds later. This is the firing
    /// site for <c>QuestEngine.OnDredgionRewardAsync</c> that unblocks quests 4725/3725/4718/3718.
    /// </summary>
    private async ValueTask DoRewardAsync()
    {
        foreach (var player in new List<Player>(PlayersInside))
        {
            var playerReward = GetPlayerReward(player);
            float abyssPoints = playerReward.Points;
            abyssPoints += player.Race == Reward.WinningRace ? Reward.WinnerPoints : Reward.LooserPoints;
            abyssPoints *= DredgionRewardRate;

            AbyssRankService.AddAp(player, (long)abyssPoints);

            // Java discards the returned rank on the quest side — any dredgion completion advances
            // the quest counter, so 0 is passed regardless of winner/loser outcome.
            await FireOnDredgionRewardAsync(player, 0, CancellationToken.None);
        }

        // note: Java also deletes every instance NPC here (npc.getController().onDelete()); this
        // port leaves that to the normal instance-destroy cleanup (InstanceService.DestroyInstance)
        // once the channel empties out below, since GeneralInstanceHandler doesn't expose a
        // scope-wide NPC-removal helper.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (_isInstanceDestroyed) return;
            foreach (var player in new List<Player>(PlayersInside))
                await MoveToInstanceExitAsync(player, CancellationToken.None);
            // note: Java also unregisters the AutoGroupService registration here
            // (AutoGroupService.unRegisterInstance) — the auto-group registration/matchmaking
            // subsystem (DredgionService) isn't ported yet, so there is nothing to unregister.
        });
    }

    protected DredgionPlayerReward GetPlayerReward(Player player)
    {
        var reward = Reward.GetPlayerReward(player.ObjectId);
        if (reward is null)
        {
            reward = new DredgionPlayerReward(player.ObjectId);
            Reward.AddPlayerReward(reward);
        }
        return reward;
    }

    public override bool OnDie(Player player, Creature? lastAttacker)
    {
        if (lastAttacker is Player killer && killer.Race != player.Race)
        {
            int points = BaseDeathPenalty;
            if (Reward.GetPointsByRace(killer.Race) < Reward.GetPointsByRace(player.Race))
                points = (int)(points * _loosingGroupMultiplier);
            else if (_loosingGroupMultiplier == 10 || GetPlayerReward(player).Points == 0)
                points = 0;

            UpdateScore(killer, player, points, pvpKill: true);
        }
        UpdateScore(player, player, -BaseDeathPenalty, pvpKill: false);

        // Default SM_DIE/SM_EMOTION handling already runs unconditionally from the combat call site
        // (CM_ATTACK/CM_CASTSPELL) regardless of this return value — see InstanceDeathHandler.
        return true;
    }

    /// <summary>See the file-header scope note: per-mob-id score tables aren't ported (no
    /// AggroList/most-damage-player attribution yet), so mob kills inside a dredgion award no
    /// score. The run still completes via the 42-minute auto-finish timer.</summary>
    public override void OnDie(Npc npc) { }

    /// <summary>Java updateScore(): awards/deducts race + per-player points, splitting across an
    /// online group when the acting player is grouped, and recalculates the losing-side score
    /// multiplier used to dampen further reprisal kills once one side is far behind.</summary>
    protected void UpdateScore(Player player, Creature? target, int points, bool pvpKill)
    {
        if (points == 0) return;

        Reward.AddPointsByRace(player.Race, points);

        var playersToGainScore = new List<Player>();
        if (target is not null && player.Group is { } group)
        {
            // note: Java's 3D group-range check (MathUtil.isIn3dRange against GroupConfig.
            // GROUP_MAX_DISTANCE) isn't ported — every non-dead online group member qualifies
            // regardless of in-instance distance from the kill.
            foreach (var member in group.Members)
                if (!member.IsAlreadyDead) playersToGainScore.Add(member);
        }
        else
        {
            playersToGainScore.Add(player);
        }

        foreach (var recipient in playersToGainScore)
            GetPlayerReward(recipient).AddPoints(points / playersToGainScore.Count);
        // note: Java's per-recipient SM_SYSTEM_MESSAGE(1400237, ...) score-gain toast is skipped —
        // no DescriptionId-based system-message plumbing wired for instance handlers yet.

        int pointDifference = Math.Abs(Reward.GetPointsByRace(Race.ASMODIANS) - Reward.GetPointsByRace(Race.ELYOS));
        _loosingGroupMultiplier = pointDifference >= 3000 ? 10f : pointDifference >= 1000 ? 1.5f : 1f;

        if (pvpKill && points > 0)
            GetPlayerReward(player).AddPvPKill();
        // note: Java's Balaur/NPC-kill branch (target is Npc && Race.DRAKAN => addBalaurKillToPlayer)
        // is unreachable here since OnDie(Npc) never calls UpdateScore (see above).

        SendScorePacket();
    }

    private void SendScorePacket()
        => BroadcastToInstance(new SM_INSTANCE_SCORE(GetRemainingTimeMs(), Reward, new List<Player>(PlayersInside)));

    private int GetRemainingTimeMs()
    {
        const long doorOpenMs = 120_000;
        const long totalMs = 2_520_000;
        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _instanceStartUtcMs;
        if (elapsed < doorOpenMs) return (int)(doorOpenMs - elapsed);
        if (elapsed < totalMs) return (int)(2_400_000 - (elapsed - doorOpenMs));
        return 0;
    }
}

[InstanceId(300110000)] // Baranath Dredgion
public sealed class BaranathDredgionInstance2 : DredgionInstance2
{
    public override void OnEnterInstance(Player player)
    {
        if (TryStartOnce()) StartInstanceTask();
        base.OnEnterInstance(player);
    }

    protected override void OpenFirstDoors()
    {
        OpenDoor(17);
        OpenDoor(18);
    }
}

[InstanceId(300210000)] // Chantra Dredgion
public sealed class ChantraDredgionInstance2 : DredgionInstance2
{
    public override void OnEnterInstance(Player player)
    {
        if (TryStartOnce()) StartInstanceTask();
        base.OnEnterInstance(player);
    }

    protected override void OpenFirstDoors()
    {
        OpenDoor(4);
        OpenDoor(173);
    }
}

[InstanceId(300440000)] // Terath Dredgion
public sealed class TerathDredgionInstance2 : DredgionInstance2
{
    public override void OnEnterInstance(Player player)
    {
        if (TryStartOnce()) StartInstanceTask();
        base.OnEnterInstance(player);
    }

    protected override void OpenFirstDoors()
    {
        OpenDoor(173);
        OpenDoor(4);
    }
}
