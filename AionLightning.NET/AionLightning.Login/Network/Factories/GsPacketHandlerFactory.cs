using AionLightning.Commons.Network;
using AionLightning.Login.Network.GameServer;
using AionLightning.Login.Network.GameServer.Clientpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Factories;

public sealed class GsPacketHandlerFactory
{
    private readonly ILogger<GsPacketHandlerFactory> _logger;

    public GsPacketHandlerFactory(ILogger<GsPacketHandlerFactory> logger)
    {
        _logger = logger;
    }

    public GsClientPacket? Resolve(byte opcode, GsConnection.GsState state)
    {
        switch (state)
        {
            case GsConnection.GsState.CONNECTED:
                return opcode switch
                {
                    0x00 => new CM_GS_AUTH(),
                    _ => Unknown(state, opcode),
                };
            case GsConnection.GsState.AUTHED:
                return opcode switch
                {
                    0x01 => new CM_ACCOUNT_AUTH(),
                    _ => Unknown(state, opcode),
                };
            default:
                return Unknown(state, opcode);
        }
    }

    private GsClientPacket? Unknown(GsConnection.GsState state, byte opcode)
    {
        _logger.LogWarning("Unknown GS packet: state={State} opcode=0x{Opcode:X2}", state, opcode);
        return null;
    }
}
