using AionLightning.Game.Dao;
using AionLightning.Game.Model.Mail;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services.Mail;

/// <summary>
/// Port of Java <c>services.mail.SystemMailService</c> — sends mail with no player sender (a system
/// sender name such as <c>"$$HS_AUCTION_MAIL"</c>), targeting a recipient by object id whether they are
/// online or offline. Java resolved the recipient via <c>PlayerDAO.loadPlayerCommonDataByName</c> (a
/// lightweight, always-available row); this port reuses <see cref="IPlayerDao.FindByObjectIdAsync"/>
/// instead since no lightweight "common data" projection exists in the C# DAO layer yet.
/// </summary>
public sealed class SystemMailService(
    IMailDao mailDao,
    IPlayerDao playerDao,
    PlayerConnectionRegistry connRegistry,
    ILogger<SystemMailService> log)
{
    private const int MaxSenderNameLength = 16;
    private const int MaxTitleLength = 20;
    private const int MaxMessageLength = 1000;
    private const int MailboxCapacity = 200;

    /// <summary>
    /// Sends a system mail to <paramref name="recipientObjectId"/>, persisting it via <see cref="IMailDao"/>
    /// and — if the recipient is online — pushing the mailbox-state notify packet immediately.
    /// Returns false (and logs why) on validation failure, unknown recipient, or full mailbox, matching
    /// Java's fail-soft <c>sendMail</c>/<c>sendSystemMail</c> contract.
    /// </summary>
    public async Task<bool> SendSystemMailAsync(
        int recipientObjectId,
        string senderName,
        string title,
        string message,
        long attachedKinah = 0,
        int attachedItemId = 0,
        long attachedItemCount = 0,
        LetterType letterType = LetterType.Normal,
        CancellationToken ct = default)
    {
        if (attachedItemId != 0 && attachedItemCount <= 0)
        {
            log.LogWarning("SystemMail rejected: attached item {ItemId} has no count", attachedItemId);
            return false;
        }

        if (!senderName.StartsWith("$$", StringComparison.Ordinal) && senderName.Length > MaxSenderNameLength)
        {
            log.LogWarning("SystemMail rejected: sender name {Sender} exceeds {Max} chars", senderName, MaxSenderNameLength);
            return false;
        }

        if (title.Length > MaxTitleLength)
            title = title[..MaxTitleLength];
        if (message.Length > MaxMessageLength)
            message = message[..MaxMessageLength];

        var recipient = await playerDao.FindByObjectIdAsync(recipientObjectId, ct);
        if (recipient is null)
        {
            log.LogInformation("SystemMail: recipient {RecipientObjectId} does not exist", recipientObjectId);
            return false;
        }

        var existingMails = await mailDao.GetReceivedMailsAsync(recipientObjectId, ct);
        if (existingMails.Count >= MailboxCapacity)
        {
            log.LogInformation("SystemMail: mailbox full for {Recipient}", recipient.Name);
            return false;
        }

        // note: Java attaches a real Item instance (ItemFactory.newItem + InventoryDAO.store) so the
        // reward keeps its full item-template data (enchant/options/etc.) once claimed from the
        // mailbox. The C# mail pipeline (like CM_SEND_MAIL) only persists the item id/count summary on
        // the MailEntry row — claiming a system-mail item attachment client-side isn't wired up yet.
        var mail = new MailEntry
        {
            SenderId = 0,
            SenderName = senderName,
            ReceiverId = recipientObjectId,
            ReceiverName = recipient.Name,
            Title = title,
            Message = message,
            AttachedItemId = attachedItemId,
            AttachedCount = attachedItemCount,
            AttachedKinah = attachedKinah,
            LetterType = (byte)letterType,
            SendDate = DateTime.UtcNow,
        };
        await mailDao.InsertAsync(mail, ct);

        var recipientConn = connRegistry.Get(recipientObjectId);
        if (recipientConn?.ActivePlayer is not null)
        {
            var mails = await mailDao.GetReceivedMailsAsync(recipientObjectId, ct);
            int unread = mails.Count(m => !m.IsRead);
            await recipientConn.SendAsync(new SM_MAIL_SERVICE(mails.Count, unread), ct);
            // note: Java also sends SM_SYSTEM_MESSAGE.STR_POSTMAN_NOTIFY for Express/BlackCloud mail;
            // no equivalent client string constant exists in the ported SM_SYSTEM_MESSAGE yet, so it's skipped.
        }

        log.LogInformation(
            "SystemMail sent: sender={Sender} recipient={Recipient} kinah={Kinah} item={ItemId}x{Count}",
            senderName, recipient.Name, attachedKinah, attachedItemId, attachedItemCount);
        return true;
    }
}
