using AionLightning.Commons.Network;
using AionLightning.Login.Dao;
using AionLightning.Login.Network.GameServer.ServerPackets;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_ACCOUNT_LIST : GsClientPacket
{
    private readonly GsConnection _conn;
    private readonly IAccountDao _accountDao;
    private string[] _accountNames = Array.Empty<string>();

    public CM_ACCOUNT_LIST(GsConnection conn, IAccountDao accountDao)
    {
        _conn = conn;
        _accountDao = accountDao;
    }

    public override void Read(ref PacketReader r)
    {
        int count = r.ReadD();
        _accountNames = new string[count];
        for (int i = 0; i < count; i++)
            _accountNames[i] = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var gsi = _conn.GameServerInfo!;
        foreach (var name in _accountNames)
        {
            var account = await _accountDao.FindByNameAsync(name, ct);
            if (account == null) continue;

            if (GameServerTable.IsAccountOnAnyGameServer(account))
            {
                await _conn.SendAsync(new SM_REQUEST_KICK_ACCOUNT(account.Id), ct);
                continue;
            }
            gsi.AddAccount(account);
        }
    }
}
