using AionLightning.Chat.Network.Aion.ServerPackets;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ClientPackets;

public sealed class CM_CHAT_INI : AionClientPacket
{
    private readonly AionClientConnection _conn;

    public CM_CHAT_INI(AionClientConnection conn) => _conn = conn;

    public override void Read(ref PacketReader r)
    {
        r.ReadC(); r.ReadH(); r.ReadD(); r.ReadD(); r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _conn.SendAsync(new SM_CHAT_INI(), ct);
}
