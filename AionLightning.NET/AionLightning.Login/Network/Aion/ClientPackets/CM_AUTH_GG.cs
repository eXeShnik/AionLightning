using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_AUTH_GG : AionClientPacket
{
    private int _sessionId;

    public override void Read(ref PacketReader r)
    {
        _sessionId = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: validate session id, set AUTHED_GG state, send SM_AUTH_GG
        return ValueTask.CompletedTask;
    }
}
