using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deletes one or more mail messages. Opcode 0x12B.</summary>
public sealed class CM_DELETE_MAIL : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IMailDao           _mailDao;

    private int[] _mailIds = [];

    public CM_DELETE_MAIL(GsClientConnection conn, IMailDao mailDao)
    {
        _conn    = conn;
        _mailDao = mailDao;
    }

    public override void Read(ref PacketReader r)
    {
        int count = r.ReadC();
        _mailIds = new int[count];
        for (int i = 0; i < count; i++)
        {
            r.ReadC();         // unk padding byte
            _mailIds[i] = r.ReadD();
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || _mailIds.Length == 0) return;

        await _mailDao.DeleteAsync(_mailIds, player.ObjectId, ct);

        var remaining = await _mailDao.GetReceivedMailsAsync(player.ObjectId, ct);
        int unread    = remaining.Count(m => !m.IsRead);
        await _conn.SendAsync(new SM_MAIL_SERVICE(_mailIds, remaining.Count, unread), ct);
    }
}
