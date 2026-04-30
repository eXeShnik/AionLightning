using AionLightning.Game.Model.Mail;

namespace AionLightning.Game.Dao;

public interface IMailDao
{
    Task<IReadOnlyList<MailEntry>> GetReceivedMailsAsync(int playerId, CancellationToken ct = default);
    Task<MailEntry?> GetByIdAsync(int mailId, CancellationToken ct = default);
    Task<int> InsertAsync(MailEntry mail, CancellationToken ct = default);
    Task MarkReadAsync(int mailId, CancellationToken ct = default);
    Task MarkAttachmentTakenAsync(int mailId, CancellationToken ct = default);
    Task DeleteAsync(int[] mailIds, int playerId, CancellationToken ct = default);
    Task<bool> HasUnreadAsync(int playerId, CancellationToken ct = default);
}
