using AionLightning.Commons.Network;
using AionLightning.Login.Network.Aion.ServerPackets;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_SERVER_LIST : AionClientPacket
{
    private readonly LoginConnection _conn;
    private int _accountId;
    private int _loginOk;

    public CM_SERVER_LIST(LoginConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        r.Skip(15);  // unused bytes
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.SessionKey?.CheckLogin(_accountId, _loginOk) != true)
        {
            await _conn.DisposeAsync();
            return;
        }

        var servers = GameServerTable.GetGameServers();
        sbyte lastServer = _conn.Account?.LastServer ?? -1;
        await _conn.SendAsync(new SM_SERVER_LIST(servers, lastServer, _conn.IP), ct);
    }
}
