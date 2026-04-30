using AionLightning.Game.Model.Mail;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class MailDaoImpl : IMailDao
{
    private readonly MySqlDataSource _db;

    public MailDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<MailEntry>> GetReceivedMailsAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<MailRow>(
            """
            SELECT id, sender_id, sender_name, receiver_id, receiver_name,
                   title, message, attached_item_id, attached_count, attached_kinah,
                   letter_type, send_date, is_read, attachment_taken
            FROM mail
            WHERE receiver_id = @playerId AND recipient_deleted = 0
            ORDER BY send_date DESC
            """, new { playerId });
        return rows.Select(ToEntry).ToList();
    }

    public async Task<MailEntry?> GetByIdAsync(int mailId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<MailRow>(
            """
            SELECT id, sender_id, sender_name, receiver_id, receiver_name,
                   title, message, attached_item_id, attached_count, attached_kinah,
                   letter_type, send_date, is_read, attachment_taken
            FROM mail WHERE id = @mailId
            """, new { mailId });
        return row is null ? null : ToEntry(row);
    }

    public async Task<int> InsertAsync(MailEntry mail, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO mail (sender_id, sender_name, receiver_id, receiver_name,
                title, message, attached_item_id, attached_count, attached_kinah,
                letter_type, send_date)
            VALUES (@SenderId, @SenderName, @ReceiverId, @ReceiverName,
                @Title, @Message, @AttachedItemId, @AttachedCount, @AttachedKinah,
                @LetterType, @SendDate);
            SELECT LAST_INSERT_ID();
            """, mail);
    }

    public async Task MarkReadAsync(int mailId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE mail SET is_read = 1 WHERE id = @mailId", new { mailId });
    }

    public async Task MarkAttachmentTakenAsync(int mailId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE mail SET attachment_taken = 1, attached_item_id = 0, attached_count = 0, attached_kinah = 0 WHERE id = @mailId",
            new { mailId });
    }

    public async Task DeleteAsync(int[] mailIds, int playerId, CancellationToken ct = default)
    {
        if (mailIds.Length == 0) return;
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE mail SET recipient_deleted = 1 WHERE id IN @mailIds AND receiver_id = @playerId",
            new { mailIds, playerId });
    }

    private static MailEntry ToEntry(MailRow r) => new()
    {
        Id              = r.id,
        SenderId        = r.sender_id,
        SenderName      = r.sender_name,
        ReceiverId      = r.receiver_id,
        ReceiverName    = r.receiver_name,
        Title           = r.title,
        Message         = r.message,
        AttachedItemId  = r.attached_item_id,
        AttachedCount   = r.attached_count,
        AttachedKinah   = r.attached_kinah,
        LetterType      = r.letter_type,
        SendDate        = r.send_date,
        IsRead          = r.is_read,
        AttachmentTaken = r.attachment_taken,
    };

    public async Task<bool> HasUnreadAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM mail WHERE receiver_id = @playerId AND recipient_deleted = 0 AND is_read = 0",
            new { playerId });
        return count > 0;
    }

    private sealed record MailRow(
        int id, int sender_id, string sender_name, int receiver_id, string receiver_name,
        string title, string message, int attached_item_id, long attached_count,
        long attached_kinah, byte letter_type, DateTime send_date, bool is_read, bool attachment_taken);
}
