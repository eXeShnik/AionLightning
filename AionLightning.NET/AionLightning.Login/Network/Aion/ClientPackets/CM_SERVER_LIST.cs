using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_SERVER_LIST : AionClientPacket
{
    private int _accountId;
    private int _loginOk;

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: validate session key, send SM_SERVER_LIST
        return ValueTask.CompletedTask;
    }
}
