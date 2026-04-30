using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client opens a mail message. Opcode 0x124.</summary>
public sealed class CM_READ_MAIL : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IMailDao           _mailDao;

    private int _mailId;

    public CM_READ_MAIL(GsClientConnection conn, IMailDao mailDao)
    {
        _conn    = conn;
        _mailDao = mailDao;
    }

    public override void Read(ref PacketReader r) => _mailId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var mail = await _mailDao.GetByIdAsync(_mailId, ct);
        if (mail is null || mail.ReceiverId != player.ObjectId) return;

        if (!mail.IsRead)
        {
            await _mailDao.MarkReadAsync(_mailId, ct);
            mail.IsRead = true;
        }

        var mails  = await _mailDao.GetReceivedMailsAsync(player.ObjectId, ct);
        int unread = mails.Count(m => !m.IsRead);
        await _conn.SendAsync(new SM_MAIL_SERVICE(mail, mails.Count, unread), ct);
    }
}
