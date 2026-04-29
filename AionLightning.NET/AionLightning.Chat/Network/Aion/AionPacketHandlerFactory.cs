using AionLightning.Chat.Network.Aion.ClientPackets;
using AionLightning.Chat.Service;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion;

public sealed class AionPacketHandlerFactory
{
    private readonly ChatService _chatService;

    public AionPacketHandlerFactory(ChatService chatService)
    {
        _chatService = chatService;
    }

    public AionClientPacket? Resolve(byte opcode, AionClientConnection.ClientState state, AionClientConnection conn)
        => (state, opcode) switch
        {
            (AionClientConnection.ClientState.CONNECTED, 0x30) => new CM_CHAT_INI(conn),
            (AionClientConnection.ClientState.CONNECTED, 0x05) => new CM_PLAYER_AUTH(conn, _chatService),
            (AionClientConnection.ClientState.AUTHED, 0x10)    => new CM_CHANNEL_REQUEST(conn, _chatService),
            (AionClientConnection.ClientState.AUTHED, 0x18)    => new CM_CHANNEL_MESSAGE(conn, _chatService),
            _ => null
        };
}
