using AionLightning.Commons.Network;
using AionLightning.Login.Network.GameServer;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_ACCOUNT_AUTH : GsClientPacket
{
    private int _accountId;
    private int _loginOk;
    private int _playOk1;
    private int _playOk2;

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        _playOk1 = r.ReadD();
        _playOk2 = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: AccountController.CheckAuth
        return ValueTask.CompletedTask;
    }
}
