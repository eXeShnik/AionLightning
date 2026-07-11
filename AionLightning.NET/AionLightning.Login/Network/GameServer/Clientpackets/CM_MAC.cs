using AionLightning.Commons.Network;
using AionLightning.Login.Dao;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

/// <summary>
/// Game server reports the client's MAC address for an account (Java CM_MAC, opcode 13).
/// </summary>
public sealed class CM_MAC : GsClientPacket
{
    private readonly GsConnection _conn;
    private readonly IAccountDao _accountDao;
    private int _accountId;
    private string _address = string.Empty;

    public CM_MAC(GsConnection conn, IAccountDao accountDao)
    {
        _conn = conn;
        _accountDao = accountDao;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _address = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!await _accountDao.UpdateLastMacAsync(_accountId, _address, ct))
            _conn.Log.LogWarning("Unable to update account_data.last_mac for account {Id}", _accountId);
    }
}
