using AionLightning.Game.Model.Mail;
using AionLightning.Game.Model.Siege;
using House = AionLightning.Game.Model.House.House;

namespace AionLightning.Game.Services.Mail;

/// <summary>
/// Port of Java <c>services.mail.MailFormatter</c> — the small set of convenience wrappers around
/// <see cref="SystemMailService"/> used by siege rewards, housing auction results, and housing
/// maintenance/tax reminders. Java resolved title/body text from a data-driven
/// <c>mail_templates.xml</c> keyed by client string-table ids (<c>DataManager.SYSTEM_MAIL_TEMPLATES</c>
/// + <c>MailTemplate</c>/<c>MailPart</c>); that template engine has no C# port yet (see
/// migration_plan.md), so this formatter builds plain-text title/body directly instead of resolving
/// client string ids. Swap the bodies below for real client string ids once the mail-template data
/// manager is ported.
/// </summary>
public sealed class MailFormatter(SystemMailService systemMail)
{
    private const string CashItemSender = "$$CASH_ITEM_MAIL";
    private const string HouseMaintenanceSender = "$$HS_OVERDUE";
    private const string HouseAuctionSender = "$$HS_AUCTION_MAIL";
    private const string AbyssRewardSender = "$$ABYSS_REWARD_MAIL";

    /// <summary>Java MailFormatter.sendBlackCloudMail — cash-shop item delivery.</summary>
    public Task<bool> SendBlackCloudMailAsync(int recipientObjectId, int itemId, int itemCount, CancellationToken ct = default)
        => systemMail.SendSystemMailAsync(recipientObjectId, CashItemSender,
            "Cash Item Delivery", "You have received an item purchased from the cash shop.",
            attachedItemId: itemId, attachedItemCount: itemCount, letterType: LetterType.BlackCloud, ct: ct);

    /// <summary>Java MailFormatter.sendHouseMaintenanceMail — overdue maintenance warnings (1st/2nd/final).</summary>
    public Task<bool> SendHouseMaintenanceMailAsync(House house, int warnCount, DateTime impoundTime, CancellationToken ct = default)
    {
        var (title, body) = warnCount switch
        {
            1 => ("Maintenance Fee Overdue",
                $"The maintenance fee for your residence (address {house.Address}) is overdue. Pay before {impoundTime:u} to avoid repossession."),
            2 => ("Second Notice: Maintenance Overdue",
                $"Your residence (address {house.Address}) is still overdue on maintenance. It will be repossessed on {impoundTime:u} if unpaid."),
            3 => ("Final Notice: Residence Repossessed",
                $"Your residence (address {house.Address}) has been repossessed due to unpaid maintenance as of {impoundTime:u}."),
            _ => (null, null),
        };
        if (title is null)
            return Task.FromResult(false);

        return systemMail.SendSystemMailAsync(house.PlayerObjectId, HouseMaintenanceSender, title, body!, ct: ct);
    }

    /// <summary>Java MailFormatter.sendHouseAuctionMail — auction bid/sale outcome, with any kinah refund attached.</summary>
    public Task<bool> SendHouseAuctionMailAsync(House house, int recipientObjectId, AuctionResult result, DateTime time,
        long returnKinah, CancellationToken ct = default)
    {
        string body = $"Auction result for residence (address {house.Address}): {result} at {time:u}.";
        return systemMail.SendSystemMailAsync(recipientObjectId, HouseAuctionSender, "House Auction Result", body,
            attachedKinah: returnKinah, ct: ct);
    }

    /// <summary>Java MailFormatter.sendAbyssRewardMail — siege/abyss reward payout (item and/or kinah).</summary>
    public Task<bool> SendAbyssRewardMailAsync(SiegeLocation siegeLocation, int recipientObjectId, AbyssSiegeLevel level,
        SiegeResult result, DateTime time, int attachedItemId, long attachedItemCount, long attachedKinah, CancellationToken ct = default)
    {
        string body = $"Siege reward for location {siegeLocation.LocationId}: {result} ({level}) at {time:u}.";
        return systemMail.SendSystemMailAsync(recipientObjectId, AbyssRewardSender, "Abyss Siege Reward", body,
            attachedKinah, attachedItemId, attachedItemCount, ct: ct);
    }
}
