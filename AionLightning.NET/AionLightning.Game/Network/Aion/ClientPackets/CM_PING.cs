using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_PING : AionClientPacket
{
    private readonly GsClientConnection _conn;

    public CM_PING(GsClientConnection conn) { _conn = conn; }

    public override void Read(ref PacketReader r) => r.ReadH(); // unk

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _conn.SendAsync(new SM_PONG(), ct);
}
