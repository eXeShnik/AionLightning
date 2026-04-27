using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_UPDATE_SESSION : AionClientPacket
{
    private int _accountId;
    private int _loginOk;
    private int _reconnectKey;

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        _reconnectKey = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: AccountController.AuthReconnectingAccount
        return ValueTask.CompletedTask;
    }
}
