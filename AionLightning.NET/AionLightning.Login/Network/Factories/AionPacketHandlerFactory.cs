using AionLightning.Commons.Network;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.Aion.ClientPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Factories;

public sealed class AionPacketHandlerFactory
{
    private readonly ILogger<AionPacketHandlerFactory> _log;
    private readonly IAccountController _accountCtrl;

    public AionPacketHandlerFactory(ILogger<AionPacketHandlerFactory> log, IAccountController accountCtrl)
    {
        _log = log;
        _accountCtrl = accountCtrl;
    }

    public AionClientPacket? Resolve(byte opcode, LoginConnection.LoginState state, LoginConnection conn)
    {
        switch (state)
        {
            case LoginConnection.LoginState.CONNECTED:
                return opcode switch
                {
                    0x07 => new CM_AUTH_GG(conn),
                    0x08 => new CM_UPDATE_SESSION(conn, _accountCtrl),
                    _ => Unknown(state, opcode),
                };
            case LoginConnection.LoginState.AUTHED_GG:
                return opcode switch
                {
                    0x0B => new CM_LOGIN(conn, _accountCtrl),
                    _ => Unknown(state, opcode),
                };
            case LoginConnection.LoginState.AUTHED_LOGIN:
                return opcode switch
                {
                    0x05 => new CM_SERVER_LIST(conn, _accountCtrl),
                    0x02 => new CM_PLAY(conn),
                    _ => Unknown(state, opcode),
                };
            default:
                return Unknown(state, opcode);
        }
    }

    private AionClientPacket? Unknown(LoginConnection.LoginState state, byte opcode)
    {
        _log.LogWarning("Unknown packet: state={State} opcode=0x{Opcode:X2}", state, opcode);
        return null;
    }
}
