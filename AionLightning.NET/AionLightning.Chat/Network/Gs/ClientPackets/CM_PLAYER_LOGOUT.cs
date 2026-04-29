using AionLightning.Chat.Service;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Gs.ClientPackets;

public sealed class CM_PLAYER_LOGOUT : AionClientPacket
{
    private readonly GsConnection _conn;
    private readonly ChatService _chatService;
    private int _playerId;

    public CM_PLAYER_LOGOUT(GsConnection conn, ChatService chatService)
    {
        _conn = conn;
        _chatService = chatService;
    }

    public override void Read(ref PacketReader r) => _playerId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _chatService.PlayerLogoutAsync(_playerId);
}
