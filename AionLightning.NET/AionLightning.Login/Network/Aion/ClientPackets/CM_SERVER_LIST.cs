using AionLightning.Commons.Network;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_SERVER_LIST : AionClientPacket
{
    private readonly LoginConnection _conn;
    private readonly IAccountController _accountCtrl;
    private int _accountId;
    private int _loginOk;

    public CM_SERVER_LIST(LoginConnection conn, IAccountController accountCtrl)
    {
        _conn = conn;
        _accountCtrl = accountCtrl;
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
            await _conn.SendAsync(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), ct);
            await _conn.DisposeAsync();
            return;
        }

        var servers = GameServerTable.GetGameServers();
        if (servers.Count == 0)
        {
            await _conn.SendAsync(new SM_LOGIN_FAIL(AionAuthResponse.NO_GS_REGISTERED), ct);
            await _conn.DisposeAsync();
            return;
        }

        _conn.Log.LogInformation("[{IP}] Server list requested: {Count} server(s) — {Details}",
            _conn.IP, servers.Count,
            string.Join(", ", servers.Select(s => $"id={s.Id} online={s.IsOnline}")));

        // Java: loadGSCharactersCount — SM_SERVER_LIST goes out once all GS report char counts
        await _accountCtrl.RequestServerListAsync(_conn, ct);
    }
}
