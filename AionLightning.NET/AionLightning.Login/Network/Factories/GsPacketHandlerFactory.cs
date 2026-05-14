using AionLightning.Commons.Network;
using AionLightning.Login.Controller;
using AionLightning.Login.Dao;
using AionLightning.Login.Network.GameServer;
using AionLightning.Login.Network.GameServer.Clientpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Factories;

public sealed class GsPacketHandlerFactory
{
    private readonly ILogger<GsPacketHandlerFactory> _logger;
    private readonly IAccountController _accountCtrl;
    private readonly IAccountDao _accountDao;

    public GsPacketHandlerFactory(ILogger<GsPacketHandlerFactory> logger, IAccountController accountCtrl, IAccountDao accountDao)
    {
        _logger = logger;
        _accountCtrl = accountCtrl;
        _accountDao = accountDao;
    }

    public GsClientPacket? Resolve(byte opcode, GsConnection.GsState state, GsConnection conn)
    {
        switch (state)
        {
            case GsConnection.GsState.CONNECTED:
                return opcode switch
                {
                    0x00 => new CM_GS_AUTH(conn),
                    _ => Unknown(state, opcode),
                };
            case GsConnection.GsState.AUTHED:
                return opcode switch
                {
                    0x01 => new CM_ACCOUNT_AUTH(conn, _accountCtrl),
                    0x03 => new CM_ACCOUNT_DISCONNECTED(conn),
                    0x04 => new CM_ACCOUNT_LIST(conn, _accountDao),
                    0x0C => new CM_GS_PONG(),
                    _ => Unknown(state, opcode),
                };
            default:
                return Unknown(state, opcode);
        }
    }

    private GsClientPacket? Unknown(GsConnection.GsState state, byte opcode)
    {
        _logger.LogDebug("Unknown GS packet: state={State} opcode=0x{Opcode:X2}", state, opcode);
        return null;
    }
}
