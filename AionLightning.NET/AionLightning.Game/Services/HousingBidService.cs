using AionLightning.Commons.Services;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.HousingBidService</c> — the house-auction/bidding subsystem: loads persisted
/// bids at startup, lets players register their house for auction and place bids on others, and closes
/// the weekly auction window (winner determination, ownership transfer, kinah refunds/mail). Mirrors
/// <see cref="HousingService"/>'s gating convention: bid/auction *data* (maps, getters) is always loaded
/// and usable, but the auction-close cron is only ever armed, and no SM_* packet ever sent, while
/// <see cref="HousingOptions.Enable"/> is true (see <see cref="ScheduleAuctionCron"/>).
///
/// Scope notes (see HousingService's own doc comment: the P2+ studio-purchase/grace-period flow is not
/// present in this port):
/// - Java's grace-period detour (a bid winner who already owns another house gets a 2-week overlap via
///   <c>AuctionResult.GraceStart/GraceFail/GraceSuccess</c>, <c>House.isInGracePeriod</c>,
///   <c>HousingService.activateBoughtHouse</c>) is skipped entirely — every won auction transfers the
///   house outright (Java's plain <c>WIN_BID</c> branch).
/// - Race-based bid-list filtering (Java resolved a land's race from its manager NPC's TribeClass, which
///   has no data-manager port here) and the Heiron/Inggison/Beluslan/Gelkmaros abyss-zone min-level
///   override (Java's WorldMapType check) are not ported; see <see cref="GetHouseBidEntries"/> and
///   <see cref="HousingAuctionOptions"/>.
/// - Java persisted the auction-extension window (<c>timeProlonged</c>) via ServerVariablesDAO so it
///   survives a server restart; that DAO has no port here, so <see cref="_timeProlonged"/> is in-memory
///   only (acceptable — it only matters for the few minutes around an active auction close).
/// - Java's <c>onPlayerLogin</c> decoded the exact pending auction result from a CSV-encoded mail title
///   produced by a client-string-table mail-template engine that has no port here (see
///   <c>MailFormatter</c>'s own doc comment); this port only detects *that* a result is pending and
///   re-triggers the client's bid-list refresh, without replaying the precise system message.
/// </summary>
public sealed class HousingBidService(
    IHouseDao houseDao,
    IHouseBidsDao bidsDao,
    HousingService housingService,
    IDataManager dataManager,
    IItemDao itemDao,
    IMailDao mailDao,
    MailFormatter mailFormatter,
    PlayerConnectionRegistry connRegistry,
    CronService cronService,
    IOptions<HousingOptions> housingOptions,
    IOptions<HousingAuctionOptions> auctionOptions,
    ILogger<HousingBidService> log)
{
    private const int KinahItemId = 182400001;
    private const string HouseAuctionSender = "$$HS_AUCTION_MAIL";

    private readonly Dictionary<int, HouseBidEntry> _houseBids = new();   // keyed by House.Id
    private readonly Dictionary<int, HouseBidEntry> _playerBids = new();  // keyed by playerId
    private readonly Dictionary<int, HouseBidEntry> _bidsByIndex = new(); // keyed by client list-slot index
    private readonly object _lock = new();
    private int _timeProlonged;

    /// <summary>Java <c>start()</c>/<c>loadBidData()</c> — loads every persisted bid row, sorts it
    /// ascending by time (mirrors <see cref="PlayerHouseBid"/>'s natural order) and rebuilds the
    /// house/player/index maps from it. Java's <c>FILL_HOUSE_BIDS_AUTO</c> admin auto-fill sweep is not
    /// ported (an ops/admin convenience, not core auction behavior). Called once at startup by
    /// <see cref="HousingBidServiceHostedService"/>, after <see cref="HousingService.LoadAsync"/>.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var bids = await bidsDao.LoadBidsAsync(ct);
        bids.Sort();

        var housesById = housingService.GetCustomHouses().ToDictionary(h => h.Id);

        lock (_lock)
        {
            _houseBids.Clear();
            _playerBids.Clear();
            _bidsByIndex.Clear();

            int entryIndex = 1;
            foreach (var bid in bids)
            {
                if (!housesById.TryGetValue(bid.HouseId, out var house))
                {
                    log.LogWarning("HousingBidService: missing house {HouseId} for player {PlayerId} bid", bid.HouseId, bid.PlayerId);
                    continue;
                }

                if (!_houseBids.TryGetValue(house.Id, out var entry))
                {
                    entry = BuildEntry(house, entryIndex, bid.BidOffer);
                    _houseBids[house.Id] = entry;
                    _bidsByIndex[entryIndex++] = entry;
                }
                else if (entry.BidPrice < bid.BidOffer)
                {
                    entry.BidPrice = bid.BidOffer;
                }

                if (bid.PlayerId != 0)
                {
                    var playerEntry = entry.Clone();
                    playerEntry.BidPrice = bid.BidOffer;
                    _playerBids[bid.PlayerId] = playerEntry;

                    entry.LastBiddingPlayer = bid.PlayerId;
                    entry.LastBidTime = new DateTimeOffset(bid.Time).ToUnixTimeMilliseconds();
                    entry.IncrementBidCount();
                }
            }
        }

        // Reconcile: a house marked SellWait with no corresponding bid entry (manual DB edit, or a bid
        // row deleted out of band) is reactivated instead of being stuck unsellable/unlivable.
        foreach (var house in housesById.Values)
        {
            if (house.PlayerObjectId == 0 || house.Status != HouseStatus.SellWait) continue;
            bool hasBid;
            lock (_lock) { hasBid = _houseBids.ContainsKey(house.Id); }
            if (hasBid) continue;

            log.LogWarning("HousingBidService: house address={Address} has SellWait status but no bid entry — reactivating", house.Address);
            house.Status = HouseStatus.Active;
            house.SellStarted = null;
            await houseDao.StoreAsync(house, ct);
        }

        log.LogInformation("HousingBidService: loaded {Count} house bid(s). Minutes till auction close: {Minutes}",
            _houseBids.Count, GetMinutesTillAuction());
    }

    /// <summary>Java <c>initSieges</c>-equivalent cron arming (<c>initSieges</c> is the siege name; here
    /// it's the auction-close schedule). No-op while <see cref="HousingOptions.Enable"/> is false — mirrors
    /// <c>SiegeService.ScheduleSieges</c>'s gating pattern exactly. Called once at startup by
    /// <see cref="HousingBidServiceHostedService"/>, after <see cref="LoadAsync"/>.</summary>
    public async Task ScheduleAuctionCron(CancellationToken ct = default)
    {
        if (!housingOptions.Value.Enable)
        {
            log.LogInformation("HousingBidService: auction engine disabled (GameServer:Housing:Enable=false) — cron not armed.");
            return;
        }

        await cronService.Schedule(() => FireAndForget(ExecuteAuctionCloseAsync(), "housing auction close"),
            auctionOptions.Value.AuctionCron, longRunningTask: true);

        log.LogInformation("HousingBidService: auction cron armed ({Cron}). Minutes till next close: {Minutes}",
            auctionOptions.Value.AuctionCron, GetMinutesTillAuction());
    }

    /// <summary>Java <c>placeBid(Player, int, long)</c>.</summary>
    public async Task PlaceBidAsync(Player player, GsClientConnection conn, int entryIndex, long bidOffer, CancellationToken ct = default)
    {
        if (!player.IsBuildingInState(PlayerHouseOwnerFlags.BiddingAllowed))
        {
            int questId = player.Race == Race.ELYOS ? 18802 : 28802;
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantOwnNotCompleteQuest(questId), ct);
            return;
        }

        int minutesLeft = GetMinutesTillAuction();
        if (minutesLeft == 0)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidTimeout(), ct);
            return;
        }

        var entry = GetBidByEntryIndex(entryIndex);
        if (entry is null) return;

        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if ((kinahItem?.Count ?? 0) < bidOffer)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
            return;
        }

        var bidHouse = housingService.GetHouseByAddress(entry.Address);
        if (bidHouse is null) return;

        if (player.ObjectId == bidHouse.PlayerObjectId)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidMyHouse(), ct);
            return;
        }

        // note: Java also blocked bidding while the player's own house is mid-grace-period
        // (House.isInGracePeriod) — skipped along with the rest of the grace/studio-swap flow (see class doc).
        int playerAddress = housingService.GetPlayerAddress(player.ObjectId);
        var playerHouse = playerAddress > 0 ? housingService.GetHouseByAddress(playerAddress) : null;

        int minLevel = GetMinBidLevel(entry.HouseType, entry.LandId);
        if (minLevel > player.Level)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidLowLevel(minLevel), ct);
            return;
        }

        if (playerHouse is not null && !playerHouse.FeePaid)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidOverdue(), ct);
            return;
        }

        if (bidOffer - entry.BidPrice >= entry.BidPrice * auctionOptions.Value.BidStepLimitPercent / 100f)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidExcessAmount(), ct);
            return;
        }

        var currentBid = GetLastPlayerBid(player.ObjectId);
        if (currentBid is not null)
        {
            if (entry.LastBiddingPlayer == player.ObjectId)
            {
                await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidSuccBidHouse(), ct);
                return;
            }
            var houseBidForCurrent = GetBidByEntryIndex(currentBid.EntryIndex);
            if (houseBidForCurrent is not null && houseBidForCurrent.BidPrice == currentBid.BidPrice)
            {
                await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidOtherHouse(), ct);
                return;
            }
        }

        if (minutesLeft < 5 && _timeProlonged < 30)
        {
            _timeProlonged += 5;
        }
        else if (!IsBiddingAllowed())
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingCantBidTimeout(), ct);
            return;
        }

        if (bidOffer <= entry.BidPrice && entry.BidCount != 0)
            return;

        await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingBidSuccess(entry.Address), ct);

        var time = DateTime.UtcNow;

        kinahItem!.Count -= bidOffer;
        await itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);

        int previousPlayer = entry.LastBiddingPlayer;
        if (previousPlayer > 0)
        {
            var prevConn = connRegistry.Get(previousPlayer);
            if (prevConn?.ActivePlayer is not null)
                await prevConn.SendAsync(SM_SYSTEM_MESSAGE.HousingBidCancel(), ct);
            await mailFormatter.SendHouseAuctionMailAsync(bidHouse, previousPlayer, AuctionResult.FailedBid, time, entry.BidPrice, ct);
        }

        lock (_lock)
        {
            entry.IncrementBidCount();
            entry.LastBiddingPlayer = player.ObjectId;
            entry.LastBidTime = new DateTimeOffset(time).ToUnixTimeMilliseconds();
            entry.BidPrice = bidOffer;

            _playerBids[player.ObjectId] = entry.Clone();
        }

        await bidsDao.AddBidAsync(player.ObjectId, bidHouse.Id, bidOffer, time, ct);

        await conn.SendAsync(SM_SYSTEM_MESSAGE.HousingPriceChange(bidOffer), ct);
        await conn.SendAsync(new SM_RECEIVE_BIDS(0), ct);
    }

    /// <summary>Java <c>addHouseToAuction(House, long)</c>/<c>addHouseToAuction(House)</c>.</summary>
    public async Task<bool> AddToAuctionAsync(House house, long? initialPrice = null, CancellationToken ct = default)
    {
        if (house.Status == HouseStatus.SellWait)
            return false;

        long price = initialPrice ?? GetDefaultAuctionPrice(house);
        house.Status = HouseStatus.SellWait;

        HouseBidEntry entry;
        lock (_lock)
        {
            int maxIndex = _bidsByIndex.Count == 0 ? 0 : _bidsByIndex.Keys.Max();
            entry = BuildEntry(house, maxIndex + 1, price);
            _bidsByIndex[entry.EntryIndex] = entry;
            _houseBids[house.Id] = entry;
        }

        var time = DateTime.UtcNow;
        house.SellStarted ??= time; // don't overwrite an existing grace-period start time

        await houseDao.StoreAsync(house, ct);
        await bidsDao.AddBidAsync(0, house.Id, price, time, ct);
        return true;
    }

    /// <summary>Java <c>removeHouseFromAuction(House, boolean)</c> — used by admin commands to pull a
    /// house back out of auction, refunding the selling player (if any) and the last bidder (if any).</summary>
    public async Task<bool> RemoveFromAuctionAsync(House house, bool noSale, CancellationToken ct = default)
    {
        HouseBidEntry? bidEntry;
        HouseBidEntry? lastPlayerBid = null;
        int lastPlayer;

        lock (_lock)
        {
            if (!_houseBids.Remove(house.Id, out bidEntry))
                return false;

            lastPlayer = bidEntry.LastBiddingPlayer;
            if (lastPlayer > 0)
                _playerBids.Remove(lastPlayer, out lastPlayerBid);
            _bidsByIndex.Remove(bidEntry.EntryIndex);
        }

        var time = DateTime.UtcNow;

        if (house.PlayerObjectId != 0)
        {
            await mailFormatter.SendHouseAuctionMailAsync(house, house.PlayerObjectId, AuctionResult.CanceledBid, time,
                bidEntry.BidPrice + bidEntry.GetRefundKinah(auctionOptions.Value.BidRefundPercent), ct);
            house.Status = HouseStatus.Active;
        }
        else
        {
            house.Status = noSale ? HouseStatus.NoSale : HouseStatus.Active;
        }

        if (lastPlayer > 0 && lastPlayerBid is not null)
            await mailFormatter.SendHouseAuctionMailAsync(house, lastPlayer, AuctionResult.CanceledBid, time, lastPlayerBid.BidPrice, ct);

        await bidsDao.DeleteHouseBidsAsync(house.Id, ct);
        await houseDao.StoreAsync(house, ct);
        return true;
    }

    /// <summary>Java <c>completeHouseSell(PlayerCommonData, House)</c>, minus the grace-period detour
    /// (see class doc) — always awards the house outright (Java's <c>WIN_BID</c> branch). Kinah for the
    /// winning bid was already escrowed at bid time (see <see cref="PlaceBidAsync"/>), so no further kinah
    /// movement happens here beyond the auction-result mail.</summary>
    public async Task<AuctionResult> CompleteHouseSellAsync(int winnerId, House obtainedHouse, CancellationToken ct = default)
    {
        const AuctionResult result = AuctionResult.WinBid;
        var time = DateTime.UtcNow;

        obtainedHouse.PlayerObjectId = winnerId;
        obtainedHouse.Status = HouseStatus.Active;
        obtainedHouse.AcquiredTime = time;
        obtainedHouse.FeePaid = true;
        obtainedHouse.NextPay = null;
        obtainedHouse.SellStarted = null;
        await houseDao.StoreAsync(obtainedHouse, ct);

        var winnerConn = connRegistry.Get(winnerId);
        if (winnerConn?.ActivePlayer is { } winnerPlayer)
        {
            winnerPlayer.Houses.Clear();
            winnerPlayer.Houses.AddRange(housingService.SearchPlayerHouses(winnerId));
            winnerPlayer.BuildingOwnerState = (byte)PlayerHouseOwnerFlags.HouseOwner;

            if (housingOptions.Value.Enable)
            {
                await winnerConn.SendAsync(new SM_HOUSE_ACQUIRE(winnerId, obtainedHouse.Address, true), ct);
                int weeksUntilDue = MaintenanceTask.ComputeWeeksUntilDue(obtainedHouse, isStudio: false, MaintenanceTask.MaintenanceCron);
                await winnerConn.SendAsync(new SM_HOUSE_OWNER_INFO(winnerPlayer, obtainedHouse, weeksUntilDue), ct);
            }
            await winnerConn.SendAsync(SM_SYSTEM_MESSAGE.HousingBidWin(obtainedHouse.Address), ct);
        }

        // note: Java's House.getController().kickVisitors(...) has no equivalent — no house
        // spawn/controller layer exists in this port (see HousingService's own doc comment).
        await mailFormatter.SendHouseAuctionMailAsync(obtainedHouse, winnerId, result, time, 0, ct);
        return result;
    }

    /// <summary>Java <c>onPlayerLogin(Player)</c> — simplified to a presence check (see class doc):
    /// if any unread auction-result mail exists, tell the client to refresh its bid list.</summary>
    public async Task OnPlayerLoginAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        var mails = await mailDao.GetReceivedMailsAsync(player.ObjectId, ct);
        bool hasPendingResult = mails.Any(m => !m.IsRead && m.SenderName == HouseAuctionSender);
        if (hasPendingResult)
            await conn.SendAsync(new SM_RECEIVE_BIDS(0), ct);
    }

    public HouseBidEntry? GetHouseBid(int houseId)
    {
        lock (_lock) return _houseBids.GetValueOrDefault(houseId);
    }

    /// <summary>Java <c>getHouseBidEntries(Race)</c> — race filtering dropped (see class doc); returns
    /// every active listing.</summary>
    public List<HouseBidEntry> GetHouseBidEntries()
    {
        lock (_lock) return _houseBids.Values.ToList();
    }

    public HouseBidEntry? GetLastPlayerBid(int playerId)
    {
        lock (_lock) return _playerBids.GetValueOrDefault(playerId);
    }

    public HouseBidEntry? GetBidByEntryIndex(int index)
    {
        lock (_lock) return _bidsByIndex.GetValueOrDefault(index);
    }

    public int GetSecondsTillAuction()
    {
        var next = GetNextFireTime(auctionOptions.Value.AuctionCron);
        if (next is null) return 0;

        int left = (int)(next.Value - DateTimeOffset.Now).TotalSeconds + _timeProlonged * 60;
        return Math.Max(left, 0);
    }

    public int GetMinutesTillAuction() => GetSecondsTillAuction() / 60;

    /// <summary>Java <c>isBiddingAllowed()</c> — simplified to "the countdown hasn't reached zero"
    /// (Java's exact day-of-week blackout-window arithmetic around the auction-close moment is not
    /// reproduced; the practical gate — no bids once the countdown expires — is preserved).</summary>
    public bool IsBiddingAllowed() => GetSecondsTillAuction() > 0;

    /// <summary>Java <c>isRegisteringAllowed()</c> — simplified identically to <see cref="IsBiddingAllowed"/>.</summary>
    public bool IsRegisteringAllowed() => GetSecondsTillAuction() > 0;

    public bool CanBidHouse(Player player, int landId)
    {
        var land = dataManager.Housing.GetLand(landId);
        var houseType = land?.DefaultBuilding?.Size ?? HouseType.HOUSE;
        return player.Level >= GetMinBidLevel(houseType, landId);
    }

    private long GetDefaultAuctionPrice(House house)
    {
        var land = dataManager.Housing.GetLandByAddress(house.Address);
        return land?.SaleOptions.GoldPrice ?? 0;
    }

    private HouseType GetHouseType(House house) =>
        dataManager.Housing.GetBuilding(house.BuildingId)?.Size ?? HouseType.HOUSE;

    private HouseBidEntry BuildEntry(House house, int index, long initialBid)
    {
        var land = dataManager.Housing.GetLandByAddress(house.Address);
        var address = dataManager.Housing.GetAddress(house.Address);
        return new HouseBidEntry(house, index, initialBid, land?.Id ?? 0, GetHouseType(house), address?.MapId ?? 0);
    }

    private int GetMinBidLevel(HouseType houseType, int landId)
    {
        var land = dataManager.Housing.GetLand(landId);
        int fallback = land?.SaleOptions.MinLevel ?? 0;

        int configured = houseType switch
        {
            HouseType.HOUSE => auctionOptions.Value.HouseMinBidLevel,
            HouseType.MANSION => auctionOptions.Value.MansionMinBidLevel,
            HouseType.ESTATE => auctionOptions.Value.EstateMinBidLevel,
            HouseType.PALACE => auctionOptions.Value.PalaceMinBidLevel,
            _ => 0,
        };
        return configured > 0 ? configured : fallback;
    }

    /// <summary>Java <c>executeTask()</c> — the weekly auction-close sweep: determine each listing's
    /// winner (if any), transfer/refund accordingly, then clear and re-list any still-unowned house.</summary>
    private async Task ExecuteAuctionCloseAsync()
    {
        if (!housingOptions.Value.Enable)
            return;

        List<HouseBidEntry> snapshot;
        lock (_lock) { snapshot = _houseBids.Values.ToList(); }

        var time = DateTime.UtcNow;

        foreach (var entry in snapshot)
        {
            var house = housingService.GetHouseByAddress(entry.Address);
            if (house is null) continue;

            if (entry.BidCount > 0)
                await ResolveSoldHouseAsync(entry, house, time);
            else if (house.PlayerObjectId != 0)
                await ResolveUnsoldPlayerHouseAsync(house, time);

            await bidsDao.DeleteHouseBidsAsync(house.Id);
        }

        lock (_lock)
        {
            _houseBids.Clear();
            _playerBids.Clear();
            _bidsByIndex.Clear();
        }

        // Re-list houses that stayed unowned after the sweep (admin-listed houses nobody won).
        foreach (var entry in snapshot)
        {
            var house = housingService.GetHouseByAddress(entry.Address);
            if (house is null || house.PlayerObjectId != 0) continue;

            house.Status = HouseStatus.NoSale;
            await AddToAuctionAsync(house);
            log.LogInformation("HousingBidService: address {Address} not sold for {Price}, re-listed", entry.Address, entry.BidPrice);
        }

        _timeProlonged = 0;
    }

    private async Task ResolveSoldHouseAsync(HouseBidEntry entry, House house, DateTime time)
    {
        int winnerId = entry.LastBiddingPlayer;

        if (house.PlayerObjectId == 0)
        {
            var result = await CompleteHouseSellAsync(winnerId, house);
            log.LogInformation("HousingBidService: address {Address} sold for {Price} (bids: {Count}; result: {Result}) to player {Winner}",
                house.Address, entry.BidPrice, entry.BidCount, result, winnerId);
            return;
        }

        if (winnerId == house.PlayerObjectId)
        {
            log.LogWarning("HousingBidService: address {Address} top bidder is its own owner — cancelling sale", house.Address);
            return;
        }

        int sellerId = house.PlayerObjectId;
        long returnKinah = entry.BidPrice + entry.GetRefundKinah(auctionOptions.Value.BidRefundPercent);

        var sellerConn = connRegistry.Get(sellerId);
        if (sellerConn?.ActivePlayer is { } sellerPlayer)
        {
            await sellerConn.SendAsync(SM_SYSTEM_MESSAGE.HousingAuctionSuccess(house.Address));
            sellerPlayer.Houses.RemoveAll(h => h.Id == house.Id);
            sellerPlayer.BuildingOwnerState = (byte)PlayerHouseOwnerFlags.BuyStudioAllowed;
        }

        await mailFormatter.SendHouseAuctionMailAsync(house, sellerId, AuctionResult.SuccessSale, time, returnKinah);

        var result2 = await CompleteHouseSellAsync(winnerId, house);
        log.LogInformation("HousingBidService: address {Address} sold by player {Seller} for {Price} (bids: {Count}; result: {Result}) to player {Winner}",
            house.Address, sellerId, entry.BidPrice, entry.BidCount, result2, winnerId);
    }

    private async Task ResolveUnsoldPlayerHouseAsync(House house, DateTime time)
    {
        var sellerConn = connRegistry.Get(house.PlayerObjectId);
        if (sellerConn?.ActivePlayer is not null)
            await sellerConn.SendAsync(SM_SYSTEM_MESSAGE.HousingAuctionFail(house.Address));

        int sellerId = house.PlayerObjectId;
        house.Status = HouseStatus.Active;
        house.SellStarted = null;
        await houseDao.StoreAsync(house);

        await mailFormatter.SendHouseAuctionMailAsync(house, sellerId, AuctionResult.FailedSale, time, 0);
        log.LogInformation("HousingBidService: address {Address} not sold, reactivated for owner {Owner}", house.Address, sellerId);
    }

    private DateTimeOffset? GetNextFireTime(string cronExpression)
    {
        try
        {
            return new CronExpression(cronExpression) { TimeZone = TimeZoneInfo.Local }.GetNextValidTimeAfter(DateTimeOffset.Now);
        }
        catch (Exception e)
        {
            log.LogError(e, "HousingBidService: invalid cron expression {Cron}", cronExpression);
            return null;
        }
    }

    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "Unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);
}
