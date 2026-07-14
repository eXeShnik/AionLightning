using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services.Siege;

/// <summary>
/// Java services.siegeservice.FortressSiege — the capture/reward/persist siege for a Fortress location.
/// </summary>
public sealed class FortressSiege : Siege<FortressLocation>
{
    private readonly GameWorld _world;
    private readonly IPlayerDao _playerDao;
    private readonly MailFormatter _mailFormatter;
    private readonly int _medalRate;

    public FortressSiege(FortressLocation location, SiegeService service, ILogger log,
        GameWorld world, IPlayerDao playerDao, MailFormatter mailFormatter, int medalRate)
        : base(location, service, log)
    {
        _world = world;
        _playerDao = playerDao;
        _mailFormatter = mailFormatter;
        _medalRate = medalRate;
    }

    public override bool IsEndless => false;

    public override void AddAbyssPoints(Player player, int abyssPoints) => SiegeCounter.AddAbyssPoints(player, abyssPoints);

    public override void AddGloryPoints(Player player, int gloryPoints) => SiegeCounter.AddGloryPoints(player, gloryPoints);

    protected override async Task OnSiegeStartAsync(CancellationToken ct)
    {
        Log.LogInformation("[SIEGE] > Siege started. [FORTRESS:{Id}] [RACE:{Race}] [LegionId:{LegionId}]",
            SiegeLocationId, Location.Race, Location.LegionId);

        Location.IsVulnerable = true;
        await BroadcastStateAsync(Location, ct);

        // note: Java's clearLocation() (kick current enemies out of the fortress zone on siege start)
        // needs the zone/knownlist framework — already deferred to P2 on FortressLocation itself (see
        // its doc comment).
        // note: Java's AbyssPointsListener/GloryPointsListener GlobalCallbackHelper registration funneled
        // every AP/GP gain server-wide into this siege's counter while it was active — no equivalent
        // exists without the dropped AOP callback framework (see CLAUDE.md). AddAbyssPoints/AddGloryPoints
        // above are ready for a future phase to call directly once player AP/GP gains are wired through.

        DeSpawnNpcs(SiegeLocationId);
        SpawnNpcs(SiegeLocationId, Location.Race, SiegeModType.SIEGE);
        InitSiegeBoss();
    }

    protected override async Task OnSiegeFinishAsync(CancellationToken ct)
    {
        var winner = SiegeCounter.GetWinnerRaceCounter();
        Log.LogInformation(
            "[SIEGE] > Siege finished. [FORTRESS:{Id}] [OLD RACE:{OldRace}] [OLD LegionId:{OldLegion}] [NEW RACE:{NewRace}] [NEW LegionId:{NewLegion}]",
            SiegeLocationId, Location.Race, Location.LegionId, winner.SiegeRace, winner.WinnerLegionId ?? 0);

        // note: AbyssPointsListener/GloryPointsListener unregister — no-op, see OnSiegeStartAsync note.

        DeSpawnNpcs(SiegeLocationId);
        Location.IsVulnerable = false;
        Location.SetUnderShield(false);

        if (BossKilled)
        {
            OnCapture();
            await BroadcastUpdateAsync(Location, ct);
        }
        else
        {
            await BroadcastStateAsync(Location, ct);
        }

        SpawnNpcs(SiegeLocationId, Location.Race, SiegeModType.PEACE);

        // Reward players and owning legion — skipped when balaur holds the fortress (nobody to reward).
        if (Location.Race != SiegeRace.BALAUR)
        {
            await GiveRewardsToLegionAsync(ct);
            var winnerRaceCounter = SiegeCounter.GetRaceCounter(Location.Race);
            await GiveRewardsToPlayersAsync(winnerRaceCounter, ct);
            await AwardAbyssPointsAsync(winnerRaceCounter, ct);
            await AwardGloryPointsAsync(winnerRaceCounter, ct);
        }

        await UpdateOutpostStatusByFortressAsync(Location, ct);
        await Service.SetOwnerAsync(SiegeLocationId, Location.Race, Location.LegionId, ct);

        // note: Java's doOnAllPlayers(unsetInsideZoneType(SIEGE) + QuestEngine.onKill for winning-race
        // players in the fortress zone) needs the zone/knownlist doOnAllPlayers framework — deferred to
        // P2, see SiegeLocation's own doc comment.
        // note: Java's global elyos/asmodian influence >=30% SiegePlayerReward boost/end-boost buff
        // applied to every online player of the opposing race is not ported here — needs the
        // SiegePlayerReward effect-apply family (P3+, out of scope for this phase).
    }

    private void OnCapture()
    {
        var winner = SiegeCounter.GetWinnerRaceCounter();
        Location.Race = winner.SiegeRace;
        var artifact = Service.GetArtifact(SiegeLocationId);
        if (artifact is not null)
            artifact.Race = winner.SiegeRace;

        if (winner.SiegeRace == SiegeRace.BALAUR)
        {
            Location.LegionId = 0;
            if (artifact is not null) artifact.LegionId = 0;
        }
        else
        {
            int legionId = winner.WinnerLegionId ?? 0;
            Location.LegionId = legionId;
            if (artifact is not null) artifact.LegionId = legionId;
        }
    }

    /// <summary>Java giveRewardsToLegion() — mails the owning legion's Brigade General the fortress's
    /// legion-reward medals, but only when the fortress successfully defended (a captured fortress has
    /// nothing to reward the previous owner with).</summary>
    private async Task GiveRewardsToLegionAsync(CancellationToken ct)
    {
        if (BossKilled)
        {
            Log.LogInformation(
                "[SIEGE] > [FORTRESS:{Id}] [RACE:{Race}] [LEGION:{Legion}] Legion Reward not sending because fortress was captured(siege boss killed).",
                SiegeLocationId, Location.Race, Location.LegionId);
            return;
        }

        if (Location.LegionId == 0)
        {
            Log.LogInformation(
                "[SIEGE] > [FORTRESS:{Id}] [RACE:{Race}] [LEGION:{Legion}] Legion Reward not sending because fortress not owned by any legion.",
                SiegeLocationId, Location.Race, Location.LegionId);
            return;
        }

        int? brigadeGeneralId = Service.GetLegion(Location.LegionId)?.BrigadeGeneralId;
        if (brigadeGeneralId is not { } generalId) return;

        foreach (var medal in Location.SiegeLegionRewards)
        {
            long count = medal.Count * (long)_medalRate;
            Log.LogInformation("[SIEGE] > [Legion Reward to: {GeneralId}] ITEM RETURN {ItemId} ITEM COUNT {Count}",
                generalId, medal.ItemId, count);
            await _mailFormatter.SendAbyssRewardMailAsync(Location, generalId, AbyssSiegeLevel.None, SiegeResult.Protect,
                DateTime.UtcNow, medal.ItemId, count, 0, ct);
        }
    }

    /// <summary>Java giveRewardsToPlayers(SiegeRaceCounter) — mails item rewards to the top-N damage
    /// dealers per reward tier, then an empty "no reward" mail to the remaining participants when the
    /// fortress was successfully defended (not captured).
    /// note: Java's trailing "rest of the players" loop pre-increments its index before reading
    /// (<c>while (i &lt; size) { i++; get(i); }</c>), which skips one player and would throw
    /// ArrayIndexOutOfBounds on the last entry — fixed here to read-then-advance instead of blindly
    /// porting that bug.</summary>
    private async Task GiveRewardsToPlayersAsync(SiegeRaceCounter winnerDamage, CancellationToken ct)
    {
        var topPlayerIds = winnerDamage.PlayerDamageCounter.Keys.ToList();
        var result = BossKilled ? SiegeResult.Occupy : SiegeResult.Defender;

        int i = 0;
        int rewardLevel = 0;
        foreach (var topGrade in Location.SiegeRewards)
        {
            rewardLevel++;
            var level = (AbyssSiegeLevel)Math.Min(rewardLevel, (int)AbyssSiegeLevel.VeteranSoldier);
            int rewardedCount = 0;
            for (; i < topPlayerIds.Count && rewardedCount < topGrade.Top; i++, rewardedCount++)
            {
                int playerId = topPlayerIds[i];
                long count = topGrade.Count * (long)_medalRate;
                Log.LogInformation("[SIEGE] > [FORTRESS:{Id}] [RACE:{Race}] Player Reward to: {PlayerId} ITEM RETURN {ItemId} ITEM COUNT {Count}",
                    SiegeLocationId, Location.Race, playerId, topGrade.ItemId, count);
                await _mailFormatter.SendAbyssRewardMailAsync(Location, playerId, level, result, DateTime.UtcNow, topGrade.ItemId, count, 0, ct);
            }
        }

        if (!BossKilled)
        {
            for (; i < topPlayerIds.Count; i++)
                await _mailFormatter.SendAbyssRewardMailAsync(Location, topPlayerIds[i], AbyssSiegeLevel.None, SiegeResult.Empty,
                    DateTime.UtcNow, 0, 0, 0, ct);
        }
    }

    /// <summary>
    /// note: Java fed the winning race's AP tally via a GlobalCallbackHelper hook (AbyssPointsListener)
    /// that observed every AP gain server-wide while the siege was active. This port has no AOP callback
    /// framework (see CLAUDE.md), so nothing currently calls <see cref="Siege.AddAbyssPoints"/> and this
    /// map is always empty — the award/persist plumbing is kept ready so a future phase can wire
    /// per-player AP gains through with a one-line change.
    /// </summary>
    private async Task AwardAbyssPointsAsync(SiegeRaceCounter winnerCounter, CancellationToken ct)
    {
        foreach (var (playerId, ap) in winnerCounter.PlayerAbyssPoints)
        {
            if (_world.GetPlayerByObjectId(playerId) is not { } player) continue;
            // AddAp's return value only reports whether the rank advanced; AbyssPoints is mutated
            // regardless, so persist unconditionally (matches Combat.Handlers.PvpKillHandler's pattern).
            AbyssRankService.AddAp(player, ap);
            await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, player.AbyssGp, player.AbyssTopRanking, ct);
        }
    }

    /// <summary>
    /// Java giveGloryPointsToPlayers — mirrors <see cref="AwardAbyssPointsAsync"/> but for the
    /// Glory Points that drive officer/general promotion (<see cref="AbyssRankService.AddGloryPoints"/>).
    /// note: like <see cref="Siege.AddAbyssPoints"/>, nothing currently calls
    /// <see cref="Siege.AddGloryPoints"/> to populate <see cref="SiegeRaceCounter.PlayerGloryPoints"/> —
    /// Java fed it via the GlobalCallbackHelper-based GloryPointsListener (see OnSiegeStartAsync note),
    /// which observed AbyssPointsService.addGp calls server-wide while the siege was active and routed
    /// them here only for players physically inside the fortress zone. That AOP-based routing has no
    /// equivalent (dropped framework, see CLAUDE.md) and the actual per-tick/per-damage GP award amounts
    /// during an active siege were not located in this pass either — both need a follow-up before this
    /// map is ever non-empty. The award/persist plumbing below is kept ready for that future phase.
    /// </summary>
    private async Task AwardGloryPointsAsync(SiegeRaceCounter winnerCounter, CancellationToken ct)
    {
        foreach (var (playerId, gp) in winnerCounter.PlayerGloryPoints)
        {
            if (_world.GetPlayerByObjectId(playerId) is not { } player) continue;
            AbyssRankService.AddGloryPoints(player, gp);
            await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, player.AbyssGp, player.AbyssTopRanking, ct);
            await _playerDao.UpdateAbyssKillStatsAsync(player.ObjectId,
                player.AbyssAllKill, player.AbyssMaxRank,
                player.AbyssDailyKill, player.AbyssDailyAp, player.AbyssDailyGp,
                player.AbyssWeeklyKill, player.AbyssWeeklyAp, player.AbyssWeeklyGp,
                player.AbyssLastKill, player.AbyssLastAp, player.AbyssLastGp, ct);
        }
    }
}
