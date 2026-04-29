using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_TIME_CHECK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private int _nanoTime;

    public CM_TIME_CHECK(GsClientConnection conn) { _conn = conn; }

    public override void Read(ref PacketReader r) => _nanoTime = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _conn.SendAsync(new SM_TIME_CHECK(_nanoTime), ct);
}
