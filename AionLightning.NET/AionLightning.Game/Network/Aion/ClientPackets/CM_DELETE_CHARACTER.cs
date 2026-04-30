using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_DELETE_CHARACTER : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao _playerDao;

    private int _objectId;

    public CM_DELETE_CHARACTER(GsClientConnection conn, IPlayerDao playerDao)
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
        // Guard: player must belong to this account
        var player = await _playerDao.FindByObjectIdAsync(_objectId, ct);
        if (player is null || player.AccountId != _conn.AccountId)
        {
            await _conn.SendAsync(new SM_DELETE_CHARACTER(0, 0), ct);
            return;
        }

        int deletionTime = await _playerDao.MarkDeletedAsync(_objectId, ct);
        await _conn.SendAsync(new SM_DELETE_CHARACTER(_objectId, deletionTime), ct);
    }
}
