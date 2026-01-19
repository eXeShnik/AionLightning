using System.Buffers;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.Aion.ClientPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Factories;

public class AionPacketHandlerFactory
{
    private readonly ILogger<AionPacketHandlerFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AionPacketHandlerFactory(ILogger<AionPacketHandlerFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public AionClientPacket? Handle(ReadOnlySequence<byte> data, LoginConnection client)
    {
        AionClientPacket? msg = null;
        var state = client.State;
        var reader = new SequenceReader<byte>(data);
        reader.TryRead(out var id);

        switch (state)
        {
            case LoginConnection.LoginState.CONNECTED:
                {
                    switch (id)
                    {
                        case 0x07:
                            msg = new CM_AUTH_GG((ILogger<CM_AUTH_GG>)_serviceProvider.GetService(typeof(ILogger<CM_AUTH_GG>)), data, client);
                            break;
                        case 0x08:
                            msg = new CM_UPDATE_SESSION((ILogger<CM_UPDATE_SESSION>)_serviceProvider.GetService(typeof(ILogger<CM_UPDATE_SESSION>)), data, client);
                            break;
                        default:
                            UnknownPacket(state, id);
                            break;
                    }
                    break;
                }
            case LoginConnection.LoginState.AUTHED_GG:
                {
                    switch (id)
                    {
                        case 0x0B:
                            msg = new CM_LOGIN((ILogger<CM_LOGIN>)_serviceProvider.GetService(typeof(ILogger<CM_LOGIN>)), data, client);
                            break;
                        default:
                            UnknownPacket(state, id);
                            break;
                    }
                    break;
                }
            case LoginConnection.LoginState.AUTHED_LOGIN:
                {
                    switch (id)
                    {
                        case 0x05:
                            msg = new CM_SERVER_LIST((ILogger<CM_SERVER_LIST>)_serviceProvider.GetService(typeof(ILogger<CM_SERVER_LIST>)), data, client);
                            break;
                        case 0x02:
                            msg = new CM_PLAY((ILogger<CM_PLAY>)_serviceProvider.GetService(typeof(ILogger<CM_PLAY>)), data, client);
                            break;
                        default:
                            UnknownPacket(state, id);
                            break;
                    }
                    break;
                }
        }

        return msg;
    }

    private void UnknownPacket(LoginConnection.LoginState state, int id)
    {
        _logger.LogWarning("Unknown packet received from client: state={state} id={id}", state, id);
    }
}
