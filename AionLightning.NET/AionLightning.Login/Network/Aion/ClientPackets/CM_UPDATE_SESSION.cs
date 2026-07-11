using AionLightning.Commons.Network;
using AionLightning.Login.Controller;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_UPDATE_SESSION : AionClientPacket
{
    private readonly LoginConnection _conn;
    private readonly IAccountController _accountCtrl;
    private int _accountId;
    private int _loginOk;
    private int _reconnectKey;

    public CM_UPDATE_SESSION(LoginConnection conn, IAccountController accountCtrl)
    {
        _conn = conn;
        _accountCtrl = accountCtrl;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        _reconnectKey = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        await _accountCtrl.AuthReconnectingAccountAsync(_accountId, _loginOk, _reconnectKey, _conn, ct);
    }
}
