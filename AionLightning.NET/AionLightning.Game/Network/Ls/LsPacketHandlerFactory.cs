using AionLightning.Game.Network.Ls.ClientPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Network.Ls;

public sealed class LsPacketHandlerFactory
{
    private readonly ILogger<LsPacketHandlerFactory> _log;
    private readonly GameAccountRegistry _registry;

    public LsPacketHandlerFactory(ILogger<LsPacketHandlerFactory> log, GameAccountRegistry registry)
    {
        _log = log;
        _registry = registry;
    }

    public LsClientPacket? Resolve(byte opcode, LsConnection.LsState state, LsConnection conn)
    {
        switch (state)
        {
            case LsConnection.LsState.CONNECTED:
                return opcode switch
                {
                    0x00 => new CM_GS_AUTH_RESPONSE(conn),
                    _ => Unknown(state, opcode),
                };
            case LsConnection.LsState.AUTHED:
                return opcode switch
                {
                    0x01 => new CM_ACCOUNT_AUTH_RESPONSE(conn, _registry),
                    0x09 => new CM_MACBAN_LIST(),
                    0x0B => new CM_LS_PING(conn),
                    _ => Unknown(state, opcode),
                };
            default:
                return Unknown(state, opcode);
        }
    }

    private LsClientPacket? Unknown(LsConnection.LsState state, byte opcode)
    {
        _log.LogDebug("Unknown LS packet: state={State} opcode=0x{Op:X2}", state, opcode);
        return null;
    }
}
