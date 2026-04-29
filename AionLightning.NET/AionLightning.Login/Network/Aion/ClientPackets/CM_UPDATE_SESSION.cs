using AionLightning.Commons.Network;
using AionLightning.Login.Network.Aion.ServerPackets;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_UPDATE_SESSION : AionClientPacket
{
    private readonly LoginConnection _conn;
    private int _accountId;
    private int _loginOk;

    public CM_UPDATE_SESSION(LoginConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        r.Skip(4);  // reconnectKey
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        // Validate reconnect key against pending reconnect accounts (M3 feature)
        // For M2: just respond with SM_UPDATE_SESSION if session matches
        if (_conn.SessionKey?.CheckLogin(_accountId, _loginOk) == true)
        {
            _conn.State = LoginConnection.LoginState.AUTHED_LOGIN;
            await _conn.SendAsync(new SM_UPDATE_SESSION(_accountId, _loginOk), ct);
        }
        else
        {
            await _conn.DisposeAsync();
        }
    }
}
