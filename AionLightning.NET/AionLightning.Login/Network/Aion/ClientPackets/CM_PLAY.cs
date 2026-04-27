using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_PLAY : AionClientPacket
{
    private int _accountId;
    private int _loginOk;
    private byte _serverId;

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _loginOk = r.ReadD();
        _serverId = r.ReadC();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: validate session key, resolve game server, send SM_PLAY_OK/FAIL
        return ValueTask.CompletedTask;
    }
}
