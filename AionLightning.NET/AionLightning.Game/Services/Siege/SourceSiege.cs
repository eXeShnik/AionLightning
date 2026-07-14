using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services.Siege;

/// <summary>
/// Java services.siegeservice.SourceSiege — the capture/reward/persist siege for a Tiamaranta source
/// (Rift of Distortion) location.
/// </summary>
public sealed class SourceSiege : Siege<SourceLocation>
{
    private readonly MailFormatter _mailFormatter;
    private readonly int _medalRate;

    public SourceSiege(SourceLocation location, SiegeService service, ILogger log, MailFormatter mailFormatter, int medalRate)
        : base(location, service, log)
    {
        _mailFormatter = mailFormatter;
        _medalRate = medalRate;
    }

    public override bool IsEndless => false;

    public override void AddAbyssPoints(Player player, int abyssPoints) => SiegeCounter.AddAbyssPoints(player, abyssPoints);

    public override void AddGloryPoints(Player player, int gloryPoints) => SiegeCounter.AddGloryPoints(player, gloryPoints);

    protected override async Task OnSiegeStartAsync(CancellationToken ct)
    {
        Log.LogInformation("[SIEGE] > Siege started. [SOURCE:{Id}] [RACE:{Race}] [LegionId:{LegionId}]",
            SiegeLocationId, Location.Race, Location.LegionId);

        Location.IsPreparation = false;
        Location.IsVulnerable = true;
        Location.SetUnderShield(true);
        await BroadcastStateAsync(Location, ct);

        // note: Java's AbyssPointsListener GlobalCallbackHelper registration — see
        // FortressSiege.OnSiegeStartAsync's equivalent note (no AOP callback framework in this port).

        DeSpawnNpcs(SiegeLocationId);
        SpawnNpcs(SiegeLocationId, Location.Race, SiegeModType.SIEGE);
        InitSiegeBoss();
    }

    protected override async Task OnSiegeFinishAsync(CancellationToken ct)
    {
        var winner = SiegeCounter.GetWinnerRaceCounter();
        Log.LogInformation(
            "[SIEGE] > Siege finished. [SOURCE:{Id}] [OLD RACE:{OldRace}] [OLD LegionId:{OldLegion}] [NEW RACE:{NewRace}] [NEW LegionId:{NewLegion}]",
            SiegeLocationId, Location.Race, Location.LegionId, winner.SiegeRace, winner.WinnerLegionId ?? 0);

        DeSpawnNpcs(SiegeLocationId);
        Location.IsVulnerable = false;
        Location.SetUnderShield(false);

        if (BossKilled)
        {
            OnCapture();
            // note: Java broadcasts an additional SM_SYSTEM_MESSAGE(1301038/1301039) ownership-change
            // announcement alongside SM_SIEGE_LOCATION_INFO here (broadcastUpdate(loc, nameId) overload)
            // — dropped, needs a DescriptionId-based SM_SYSTEM_MESSAGE overload not ported yet (P3+).
            await BroadcastUpdateAsync(Location, ct);
        }
        else
        {
            await BroadcastStateAsync(Location, ct);
        }

        SpawnNpcs(SiegeLocationId, Location.Race, SiegeModType.PEACE);

        if (Location.Race != SiegeRace.BALAUR)
            await GiveRewardsToPlayersAsync(SiegeCounter.GetRaceCounter(Location.Race), ct);

        await Service.SetOwnerAsync(SiegeLocationId, Location.Race, Location.LegionId, ct);

        // note: Java's updateTiamarantaRiftsStatus(false, false) — the Tiamaranta Eye infiltration-route
        // rift subsystem is out of scope for this phase (see SourceLocation/SiegeShield P2 notes).
    }

    private void OnCapture()
    {
        var winner = SiegeCounter.GetWinnerRaceCounter();
        Location.Race = winner.SiegeRace;
        Location.LegionId = winner.SiegeRace == SiegeRace.BALAUR ? 0 : winner.WinnerLegionId ?? 0;
    }

    /// <summary>Java giveRewardsToPlayers(SiegeRaceCounter) — always SiegeResult.Occupy, unlike
    /// FortressSiege (a source can only ever be "captured", never "defended" — Java has no Defender/Empty
    /// branch here).</summary>
    private async Task GiveRewardsToPlayersAsync(SiegeRaceCounter winnerDamage, CancellationToken ct)
    {
        var topPlayerIds = winnerDamage.PlayerDamageCounter.Keys.ToList();

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
                Log.LogInformation("[SIEGE] > [SOURCE:{Id}] [RACE:{Race}] Player Reward to: {PlayerId} ITEM RETURN {ItemId} ITEM COUNT {Count}",
                    SiegeLocationId, Location.Race, playerId, topGrade.ItemId, count);
                await _mailFormatter.SendAbyssRewardMailAsync(Location, playerId, level, SiegeResult.Occupy, DateTime.UtcNow,
                    topGrade.ItemId, count, 0, ct);
            }
        }
    }
}
