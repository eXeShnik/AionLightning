using AionLightning.Chat.Configs.Options;
using AionLightning.Chat.Network.Gs.ClientPackets;
using AionLightning.Commons.Network;
using Microsoft.Extensions.Options;

namespace AionLightning.Chat.Network.Gs;

public sealed class GsPacketHandlerFactory
{
    private readonly ChatAuthOptions _authOpts;
    private readonly ChatNetworkOptions _netOpts;
    private readonly Service.ChatService _chatService;

    public GsPacketHandlerFactory(
        IOptions<ChatAuthOptions> authOpts,
        IOptions<ChatNetworkOptions> netOpts,
        Service.ChatService chatService)
    {
        _authOpts = authOpts.Value;
        _netOpts = netOpts.Value;
        _chatService = chatService;
    }

    public AionClientPacket? Resolve(byte opcode, GsConnection.GsState state, GsConnection conn)
        => (state, opcode) switch
        {
            (GsConnection.GsState.CONNECTED, 0x00) => new CM_CS_AUTH(conn, _authOpts, _netOpts),
            (GsConnection.GsState.AUTHED, 0x01)    => new CM_PLAYER_AUTH(conn, _chatService),
            (GsConnection.GsState.AUTHED, 0x02)    => new CM_PLAYER_LOGOUT(conn, _chatService),
            (GsConnection.GsState.AUTHED, 0x03)    => new CM_PLAYER_GAG(conn, _chatService),
            _ => null
        };
}
