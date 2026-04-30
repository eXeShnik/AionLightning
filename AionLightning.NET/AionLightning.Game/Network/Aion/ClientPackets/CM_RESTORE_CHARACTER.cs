using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_RESTORE_CHARACTER : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao _playerDao;

    private int _objectId;

    public CM_RESTORE_CHARACTER(GsClientConnection conn, IPlayerDao playerDao)
    {
        _conn      = conn;
        _playerDao = playerDao;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();           // playOk2, ignored
        _objectId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        bool ok = await _playerDao.CancelDeletionAsync(_objectId, _conn.AccountId, ct);
        await _conn.SendAsync(new SM_RESTORE_CHARACTER(ok ? _objectId : 0, success: ok), ct);
    }
}
