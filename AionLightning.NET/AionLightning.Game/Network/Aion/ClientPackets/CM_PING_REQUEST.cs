using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client latency ping. Server replies with SM_PING_RESPONSE. Opcode 0x105.</summary>
public sealed class CM_PING_REQUEST : AionClientPacket
{
    private readonly GsClientConnection _conn;

    public CM_PING_REQUEST(GsClientConnection conn) { _conn = conn; }

    public override void Read(ref PacketReader r) { }

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _conn.SendAsync(new SM_PING_RESPONSE(), ct);
}
