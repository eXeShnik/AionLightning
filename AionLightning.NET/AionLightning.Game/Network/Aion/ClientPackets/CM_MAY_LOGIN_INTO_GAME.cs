using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_MAY_LOGIN_INTO_GAME : AionClientPacket
{
    private readonly GsClientConnection _conn;

    public CM_MAY_LOGIN_INTO_GAME(GsClientConnection conn) { _conn = conn; }

    public override void Read(ref PacketReader r) { /* empty */ }

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _conn.SendAsync(new SM_MAY_LOGIN_INTO_GAME(), ct);
}
