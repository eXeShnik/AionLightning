using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Cs.ClientPackets;

namespace AionLightning.Game.Network.Cs;

public sealed class CsPacketHandlerFactory
{
    private readonly PlayerConnectionRegistry _connRegistry;

    public CsPacketHandlerFactory(PlayerConnectionRegistry connRegistry)
    {
        _connRegistry = connRegistry;
    }

    public AionClientPacket? Resolve(byte opcode, CsConnection.CsState state, CsConnection conn)
        => (state, opcode) switch
        {
            (CsConnection.CsState.CONNECTED, 0x00) => new CM_CS_AUTH_RESPONSE(conn),
            (CsConnection.CsState.AUTHED,    0x01) => new CM_CS_PLAYER_AUTH_RESPONSE(conn, _connRegistry),
            _ => null
        };
}
