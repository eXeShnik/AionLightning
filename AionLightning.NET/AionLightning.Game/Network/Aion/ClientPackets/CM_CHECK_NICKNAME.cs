using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CHECK_NICKNAME : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao _playerDao;

    private string _name = string.Empty;

    public CM_CHECK_NICKNAME(GsClientConnection conn, IPlayerDao playerDao)
    {
        _conn      = conn;
        _playerDao = playerDao;
    }

    public override void Read(ref PacketReader r) => _name = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        bool taken = await _playerDao.ExistsByNameAsync(_name, ct);
        byte responseCode = taken ? (byte)10 : (byte)0;
        await _conn.SendAsync(new SM_NICKNAME_CHECK_RESPONSE(responseCode), ct);
    }
}
