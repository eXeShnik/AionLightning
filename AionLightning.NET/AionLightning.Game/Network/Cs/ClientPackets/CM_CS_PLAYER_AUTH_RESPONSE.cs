using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Cs.ClientPackets;

public sealed class CM_CS_PLAYER_AUTH_RESPONSE : AionClientPacket
{
    private readonly CsConnection _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private int _playerId;
    private byte[] _token = [];

    public CM_CS_PLAYER_AUTH_RESPONSE(CsConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _playerId = r.ReadD();
        int tokenLen = r.ReadC();
        _token = r.ReadB(tokenLen);
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var gsConn = _connRegistry.Get(_playerId);
        if (gsConn is null) return;

        await gsConn.SendAsync(new SM_CHAT_INIT(_token), ct);
    }
}
