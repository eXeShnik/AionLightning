using AionLightning.Commons.Network;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.Aion.ClientPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Factories;

public sealed class AionPacketHandlerFactory
{
    private readonly ILogger<AionPacketHandlerFactory> _logger;

    public AionPacketHandlerFactory(ILogger<AionPacketHandlerFactory> logger)
    {
        _logger = logger;
    }

    public AionClientPacket? Resolve(byte opcode, LoginConnection.LoginState state)
    {
        switch (state)
        {
            case LoginConnection.LoginState.CONNECTED:
                return opcode switch
                {
                    0x07 => new CM_AUTH_GG(),
                    0x08 => new CM_UPDATE_SESSION(),
                    _ => Unknown(state, opcode),
                };
            case LoginConnection.LoginState.AUTHED_GG:
                return opcode switch
                {
                    0x0B => new CM_LOGIN(),
                    _ => Unknown(state, opcode),
                };
            case LoginConnection.LoginState.AUTHED_LOGIN:
                return opcode switch
                {
                    0x05 => new CM_SERVER_LIST(),
                    0x02 => new CM_PLAY(),
                    _ => Unknown(state, opcode),
                };
            default:
                return Unknown(state, opcode);
        }
    }

    private AionClientPacket? Unknown(LoginConnection.LoginState state, byte opcode)
    {
        _logger.LogWarning("Unknown packet: state={State} opcode=0x{Opcode:X2}", state, opcode);
        return null;
    }
}
