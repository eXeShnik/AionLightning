using AionLightning.Commons.Network;
using AionLightning.Login.Network.Aion.ServerPackets;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_PLAY : AionClientPacket
{
    private readonly LoginConnection _conn;
    private int _accountId;
    private int _loginOk;
    private byte _serverId;

    public CM_PLAY(LoginConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        _serverId = r.ReadC();
        r.Skip(14);  // unused bytes
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.SessionKey?.CheckLogin(_accountId, _loginOk) != true)
        {
            await _conn.SendAsync(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), ct);
            await _conn.DisposeAsync();
            return;
        }

        var gsi = GameServerTable.GetGameServerInfo(_serverId);
        if (gsi == null || !gsi.IsOnline)
        {
            await _conn.SendAsync(new SM_PLAY_FAIL(AionAuthResponse.SERVER_DOWN), ct);
            return;
        }

        if (gsi.IsFull())
        {
            await _conn.SendAsync(new SM_PLAY_FAIL(AionAuthResponse.SERVER_FULL), ct);
            return;
        }

        _conn.JoinedGs = gsi;
        var sk = _conn.SessionKey!;
        await _conn.SendAsync(new SM_PLAY_OK(sk.PlayOk1, sk.PlayOk2, _serverId), ct);
    }
}
