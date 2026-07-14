using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.House;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>model.house.MaintenanceTask</c> — the weekly housing rent sweep. For every owned,
/// active custom house it flips a stale <c>fee_paid</c> flag to false once the due date has passed, then
/// escalates unpaid houses through two mail warnings and, a day after the second warning, impounds the
/// house (revokes ownership, re-lists it for auction via <see cref="HousingBidService"/>). Faithful to
/// Java: this sweep never auto-deducts kinah — rent is only ever paid via the player-initiated
/// <c>CM_HOUSE_PAY_RENT</c> (see that packet). Mirrors <see cref="HousingBidService"/>'s gating
/// convention exactly: the cron is only ever armed while <see cref="HousingOptions.Enable"/> is true.
/// </summary>
public sealed class MaintenanceTask(
    IHouseDao houseDao,
    HousingService housingService,
    HousingBidService bidService,
    IPlayerDao playerDao,
    MailFormatter mailFormatter,
    PlayerConnectionRegistry connRegistry,
    CronService cronService,
    IOptions<HousingOptions> housingOptions,
    ILogger<MaintenanceTask> log)
{
    /// <summary>Java <c>HousingConfig.HOUSE_MAINTENANCE_TIME</c>'s shipped default (weekly, Monday
    /// 00:00). note: no config source (appsettings key or data file) carries this key yet in this port —
    /// hardcoded until one exists, mirroring <see cref="HousingAuctionOptions"/>'s AuctionCron/
    /// RegisterEndCron precedent for the sibling auction cron. Exposed as a public const (rather than an
    /// IOptions-bound property) so <see cref="GetPeriod"/>/<see cref="GetNextRunTime"/> stay static,
    /// DI-free pure functions callable from SM_HOUSE_OWNER_INFO/CM_HOUSE_PAY_RENT without depending on a
    /// MaintenanceTask instance (which would create a circular dependency: MaintenanceTask already depends
    /// on HousingService, and HousingService constructs SM_HOUSE_OWNER_INFO).</summary>
    public const string MaintenanceCron = "0 0 0 ? * MON";

    /// <summary>Java <c>initSieges</c>-equivalent cron arming. No-op while <see cref="HousingOptions.Enable"/>
    /// is false — mirrors <see cref="HousingBidService.ScheduleAuctionCron"/>'s gating pattern exactly.
    /// Called once at startup by <see cref="MaintenanceTaskHostedService"/>.</summary>
    public async Task ScheduleMaintenanceCron(CancellationToken ct = default)
    {
        if (!housingOptions.Value.Enable)
        {
            log.LogInformation("MaintenanceTask: disabled (GameServer:Housing:Enable=false) — cron not armed.");
            return;
        }

        await cronService.Schedule(() => FireAndForget(ExecuteAsync(), "house maintenance"), MaintenanceCron, longRunningTask: true);
        log.LogInformation("MaintenanceTask: cron armed ({Cron}).", MaintenanceCron);
    }

    /// <summary>Java <c>preRun()</c> + <c>executeTask()</c> combined into a single sweep (this port has
    /// no separate pre/post cron lifecycle — see <see cref="CronService.Schedule"/>). Also callable
    /// directly (e.g. by tooling/tests) — gated behind <see cref="HousingOptions.Enable"/> regardless of
    /// caller, matching every other housing side-effect in this port.</summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!housingOptions.Value.Enable)
            return;

        var now = DateTimeOffset.UtcNow;
        var period = GetPeriod(MaintenanceCron);
        var previousRun = now - period;        // usually a week ago
        var beforePreviousRun = previousRun - period; // usually two weeks ago

        var dueHouses = new List<House>();
        foreach (var house in housingService.GetCustomHouses())
        {
            if (house.Status == HouseStatus.Inactive) continue;
            if (house.PlayerObjectId == 0) continue;

            if (house.FeePaid)
            {
                if (house.NextPay is null || house.NextPay.Value < now)
                {
                    house.FeePaid = false;
                    // if never paid, just set time to the next period (Java's comment/behavior)
                    house.NextPay ??= GetNextRunTime(MaintenanceCron).UtcDateTime;
                    await houseDao.StoreAsync(house, ct);
                }
                else
                {
                    continue; // fee paid, due date still in the future
                }
            }
            dueHouses.Add(house);
        }

        log.LogInformation("MaintenanceTask: executing house maintenance. Due houses: {Count}", dueHouses.Count);

        foreach (var house in dueHouses)
        {
            if (house.FeePaid) continue; // paid between the two passes above

            var conn = connRegistry.Get(house.PlayerObjectId);
            var onlinePlayer = conn?.ActivePlayer;
            if (onlinePlayer is null && await playerDao.FindByObjectIdAsync(house.PlayerObjectId, ct) is null)
            {
                log.LogWarning("MaintenanceTask: house {Address} had player {Owner} assigned but no player exists — auctioned.",
                    house.Address, house.PlayerObjectId);
                await PutHouseToAuctionAsync(house, null, ct);
                continue;
            }

            var payTime = new DateTimeOffset(house.NextPay!.Value, TimeSpan.Zero);
            DateTimeOffset impoundTime;
            int warnCount;

            if (payTime <= beforePreviousRun)
            {
                var plusDay = beforePreviousRun - TimeSpan.FromDays(1);
                if (payTime <= plusDay)
                {
                    // player didn't pay after the second warning and one more day passed
                    impoundTime = now;
                    warnCount = 3;
                    await PutHouseToAuctionAsync(house, conn, ct);
                }
                else
                {
                    impoundTime = now + TimeSpan.FromDays(1);
                    warnCount = 2;
                }
            }
            else if (payTime <= previousRun)
            {
                // player didn't pay for one period
                impoundTime = now + period + TimeSpan.FromDays(1);
                warnCount = 1;
            }
            else
            {
                continue; // should not happen
            }

            if (onlinePlayer is not null)
            {
                var message = warnCount == 3 ? SM_SYSTEM_MESSAGE.HousingSequestrate() : SM_SYSTEM_MESSAGE.HousingOverdue();
                await conn!.SendAsync(message, ct);
            }

            await mailFormatter.SendHouseMaintenanceMailAsync(house, warnCount, impoundTime.UtcDateTime, ct);
        }
    }

    /// <summary>Java <c>putHouseToAuction(House, PlayerCommonData)</c>, minus the building-switch-back
    /// step (Java's <c>House.revokeOwner()</c> also resets <c>acquiredTime</c> to null and switches back
    /// to the land's default building if a non-default one was equipped — this port's House.AcquiredTime
    /// is non-nullable and no building-switch flow exists yet, P2+, so both are left as-is).</summary>
    private async Task PutHouseToAuctionAsync(House house, GsClientConnection? conn, CancellationToken ct)
    {
        house.PlayerObjectId = 0;
        house.FeePaid = true;
        house.NextPay = null;
        house.SellStarted = null;
        await houseDao.StoreAsync(house, ct);
        await bidService.AddToAuctionAsync(house, ct: ct);

        log.LogInformation("MaintenanceTask: house {Address} overdue, put to auction.", house.Address);

        if (conn?.ActivePlayer is { } player)
        {
            player.Houses.RemoveAll(h => h.Id == house.Id);
            player.BuildingOwnerState = (byte)PlayerHouseOwnerFlags.BuyStudioAllowed;
            await conn.SendAsync(new SM_HOUSE_ACQUIRE(player.ObjectId, house.Address, false), ct);
            await conn.SendAsync(new SM_HOUSE_OWNER_INFO(player, null, 0), ct);
        }
    }

    /// <summary>Java <c>SM_HOUSE_OWNER_INFO.writeImpl</c>'s maintenance-weeks branch, extracted as a pure
    /// function so the packet itself stays free of service dependencies. Callers resolve
    /// <paramref name="isStudio"/> via their own <c>IDataManager</c> (studios never owe rent).</summary>
    public static int ComputeWeeksUntilDue(House? activeHouse, bool isStudio, string maintenanceCron)
    {
        if (activeHouse is null || !activeHouse.FeePaid || isStudio)
            return 0;

        var period = GetPeriod(maintenanceCron);
        double diffMs = activeHouse.NextPay is { } nextPay
            ? (nextPay - GetNextRunTime(maintenanceCron).UtcDateTime).TotalMilliseconds
            : period.TotalMilliseconds; // never paid yet — Java's "assume next period" fallback

        if (diffMs < 0)
            return 0;

        int weeks = (int)Math.Round(diffMs / period.TotalMilliseconds);
        if (DateTime.UtcNow.DayOfWeek != DayOfWeek.Sunday) // Java's client-side week-count hack (auction day)
            weeks++;
        return weeks;
    }

    /// <summary>Java <c>AbstractCronTask.getPeriod()</c> — the interval between two consecutive cron
    /// fires, computed live from the cron expression (no persisted <c>server_variables</c> row exists in
    /// this port — see <see cref="HousingBidService.GetNextFireTime"/>'s identical simplification for the
    /// sibling auction cron).</summary>
    public static TimeSpan GetPeriod(string cronExpression)
    {
        var expr = new CronExpression(cronExpression) { TimeZone = TimeZoneInfo.Local };
        var first = expr.GetNextValidTimeAfter(DateTimeOffset.UtcNow)!.Value;
        var second = expr.GetNextValidTimeAfter(first)!.Value;
        return second - first;
    }

    /// <summary>Java <c>AbstractCronTask.getRunTime()</c> — at rest (outside of an active execution) this
    /// is always "the next scheduled fire time", which is exactly what <c>saveNextRunTime()</c> persists
    /// after each run. Computed live here instead of read from a persisted DB variable (see
    /// <see cref="GetPeriod"/>'s doc comment).</summary>
    public static DateTimeOffset GetNextRunTime(string cronExpression)
    {
        var expr = new CronExpression(cronExpression) { TimeZone = TimeZoneInfo.Local };
        return expr.GetNextValidTimeAfter(DateTimeOffset.UtcNow) ?? DateTimeOffset.UtcNow;
    }

    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "Unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);
}
