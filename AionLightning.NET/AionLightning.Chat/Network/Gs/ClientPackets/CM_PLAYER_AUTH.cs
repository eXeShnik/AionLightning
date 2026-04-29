using AionLightning.Chat.Network.Gs.ServerPackets;
using AionLightning.Chat.Service;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Gs.ClientPackets;

public sealed class CM_PLAYER_AUTH : AionClientPacket
{
    private readonly GsConnection _conn;
    private readonly ChatService _chatService;

    private int _playerId;
    private string _playerLogin = string.Empty;
    private string _nick = string.Empty;

    public CM_PLAYER_AUTH(GsConnection conn, ChatService chatService)
    {
        _conn = conn;
        _chatService = chatService;
    }

    public override void Read(ref PacketReader r)
    {
        _playerId = r.ReadD();
        _playerLogin = r.ReadS();
        _nick = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var chatClient = _chatService.RegisterPlayer(_playerId, _playerLogin, _nick);
        await _conn.SendAsync(new SM_PLAYER_AUTH_RESPONSE(chatClient.ClientId, chatClient.Token), ct);
    }
}
