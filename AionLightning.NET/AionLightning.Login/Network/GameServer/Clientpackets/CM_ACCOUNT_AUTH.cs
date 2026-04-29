using AionLightning.Commons.Network;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_ACCOUNT_AUTH : GsClientPacket
{
    private readonly GsConnection _conn;
    private readonly IAccountController _accountCtrl;
    private SessionKey _sessionKey = null!;

    public CM_ACCOUNT_AUTH(GsConnection conn, IAccountController accountCtrl)
    {
        _conn = conn;
        _accountCtrl = accountCtrl;
    }

    public override void Read(ref PacketReader r)
    {
        int accountId = r.ReadD();
        int loginOk   = r.ReadD();
        int playOk1   = r.ReadD();
        int playOk2   = r.ReadD();
        _sessionKey = new SessionKey(accountId, loginOk, playOk1, playOk2);
    }

    public override ValueTask RunAsync(CancellationToken ct)
        => new(_accountCtrl.CheckAuthAsync(_sessionKey, _conn, ct));
}
