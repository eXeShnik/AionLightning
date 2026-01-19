using System;
using AionLightning.Commons.Network;
using AionLightning.LoginServer.Network.Gameserver;
using AionLightning.LoginServer.Network.Gameserver.Clientpackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Factories
{
    public class GsPacketHandlerFactory
    {
        private static readonly ILogger<GsPacketHandlerFactory> _log = new LoggerFactory().CreateLogger<GsPacketHandlerFactory>();

        public static GsClientPacket Handle(ByteBuffer data, GsConnection client)
        {
            GsClientPacket packet = null;
            int id = data.Get();

            switch (client.ConnectionState)
            {
                case GsConnection.State.CONNECTED:
                    switch (id)
                    {
                        case 0:
                            packet = new CM_GS_AUTH(data, client);
                            break;
                        default:
                            UnknownPacket(client.ConnectionState, id);
                            break;
                    }
                    break;
                case GsConnection.State.AUTHED:
                    switch (id)
                    {
                        case 1:
                            packet = new CM_ACCOUNT_AUTH(data, client);
                            break;
                        case 2:
                            packet = new CM_ACCOUNT_RECONNECT_KEY(data, client);
                            break;
                        case 3:
                            packet = new CM_ACCOUNT_DISCONNECTED(data, client);
                            break;
                        case 4:
                            packet = new CM_ACCOUNT_LIST(data, client);
                            break;
                        case 5:
                            packet = new CM_LS_CONTROL(data, client);
                            break;
                        case 6:
                            packet = new CM_BAN(data, client);
                            break;
                        case 7:
                            packet = new CM_GS_CHARACTER(data, client);
                            break;
                        case 8:
                            packet = new CM_MAC(data, client);
                            break;
                        case 9:
                            packet = new CM_PREMIUM_CONTROL(data, client);
                            break;
                        case 10:
                            packet = new CM_PTRANSFER_CONTROL(data, client);
                            break;
                        case 11:
                            packet = new CM_GS_PONG(data, client);
                            break;
                        case 12:
                            packet = new CM_MACBAN_CONTROL(data, client);
                            break;
                        case 13:
                            packet = new CM_ACCOUNT_TOLL_INFO(data, client);
                            break;
                        default:
                            UnknownPacket(client.ConnectionState, id);
                            break;
                    }
                    break;
            }
            return packet;
        }

        private static void UnknownPacket(GsConnection.State state, int id)
        {
            _log.LogWarning($"Unknown packet received from GameServer: 0x{id:X2} in state {state}");
        }
    }
}
