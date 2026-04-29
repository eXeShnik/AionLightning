using AionLightning.Commons.Network;
using AionLightning.Game.Network.Ls.ServerPackets;

namespace AionLightning.Game.Network.Ls.ClientPackets;

public sealed class CM_LS_PING : LsClientPacket
{
    private readonly LsConnection _conn;

    public CM_LS_PING(LsConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        await _conn.SendAsync(new SM_LS_PONG(_conn.Info.Id), ct);
    }
}
