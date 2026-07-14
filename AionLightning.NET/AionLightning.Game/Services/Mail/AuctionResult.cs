namespace AionLightning.Game.Services.Mail;

/// <summary>Port of Java services.mail.AuctionResult — housing auction outcome, used in the auction-result mail.</summary>
public enum AuctionResult
{
    FailedBid = 0,
    CanceledBid = 1,
    FailedSale = 2,
    SuccessSale = 3,
    WinBid = 4,
    GraceStart = 5,
    GraceFail = 6,
    GraceSuccess = 7,
}
