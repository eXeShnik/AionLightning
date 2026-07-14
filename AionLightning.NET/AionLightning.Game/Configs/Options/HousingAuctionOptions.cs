namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Sibling options record to <see cref="HousingOptions"/>, scoped to the housing auction/bidding
/// subsystem (Java <c>HousingConfig</c>'s auction-related keys). Bound under
/// <c>GameServer:Housing:Auction</c>. The auction-close cron is only ever armed while
/// <see cref="HousingOptions.Enable"/> is true (see HousingBidServiceHostedService) — these values are
/// otherwise inert.
/// </summary>
public sealed record HousingAuctionOptions
{
    /// <summary>Java <c>HousingConfig.HOUSE_AUCTION_TIME</c> — weekly cron firing the auction-close sweep.</summary>
    public string AuctionCron { get; init; } = "0 5 12 ? * SUN";

    /// <summary>Java <c>HousingConfig.HOUSE_REGISTER_END</c> — cron marking the cutoff after which new
    /// auction registrations are rejected until the next auction window opens.</summary>
    public string RegisterEndCron { get; init; } = "0 0 0 ? * SAT";

    /// <summary>Java <c>HousingConfig.BID_REFUND_PERCENT</c> — fraction of a superseded/cancelled bid's
    /// price refunded on top of the principal.</summary>
    public float BidRefundPercent { get; init; } = 0.3f;

    /// <summary>Java <c>HousingConfig.HOUSE_AUCTION_BID_LIMIT</c> — max percentage a new bid may exceed
    /// the current top bid by, in one step.</summary>
    public float BidStepLimitPercent { get; init; } = 100f;

    /// <summary>Java <c>CM_REGISTER_HOUSE</c>'s hardcoded 0.3f registration fee fraction of the listed price.</summary>
    public float RegisterFeePercent { get; init; } = 0.3f;

    public int HouseMinBidLevel { get; init; } = 21;
    public int MansionMinBidLevel { get; init; } = 30;
    public int EstateMinBidLevel { get; init; } = 40;
    public int PalaceMinBidLevel { get; init; } = 50;

    // note: Java's per-house-type minimum-bid kinah overrides (HousingConfig.HOUSE_MIN_BID/MANSION_MIN_BID/
    // ESTATE_MIN_BID/PALACE_MIN_BID, each defaulting to 0 = "use the land's <sale gold_price>") and the
    // Heiron/Inggison/Beluslan/Gelkmaros abyss-zone min-level override (Java's WorldMapType check in
    // getMinBidLevel) are not ported — this port always falls back to HousingLand.SaleOptions for the
    // default auction price and never special-cases abyss-zone lands for min level. Neither WorldMapType
    // nor a per-type kinah-override config existed anywhere else in this codebase to build on.
}
