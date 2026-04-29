using AionLightning.Commons.Network;
using AionLightning.Login.Network.Aion.ServerPackets;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_AUTH_GG : AionClientPacket
{
    private readonly LoginConnection _conn;
    private int _sessionId;

    public CM_AUTH_GG(LoginConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _sessionId = r.ReadD();
        r.Skip(4 * 4);  // 4 unused ints
        r.Skip(11);     // 11 unused bytes
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_sessionId != _conn.SessionId)
        {
            await _conn.DisposeAsync();
            return;
        }

        _conn.State = LoginConnection.LoginState.AUTHED_GG;
        await _conn.SendAsync(new SM_AUTH_GG(_conn.SessionId), ct);
    }
}
