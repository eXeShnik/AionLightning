using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_QUIT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private bool _logout;

    public CM_QUIT(GsClientConnection conn) { _conn = conn; }

    public override void Read(ref PacketReader r) => _logout = r.ReadC() == 1;

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        await _conn.SendAsync(new SM_QUIT_RESPONSE(), ct);
        await _conn.DisposeAsync();
    }
}
