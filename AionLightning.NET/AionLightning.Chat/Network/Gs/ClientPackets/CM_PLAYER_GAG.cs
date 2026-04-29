using AionLightning.Chat.Service;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Gs.ClientPackets;

public sealed class CM_PLAYER_GAG : AionClientPacket
{
    private readonly GsConnection _conn;
    private readonly ChatService _chatService;
    private int _playerId;
    private long _gagTime;

    public CM_PLAYER_GAG(GsConnection conn, ChatService chatService)
    {
        _conn = conn;
        _chatService = chatService;
    }

    public override void Read(ref PacketReader r)
    {
        _playerId = r.ReadD();
        _gagTime = r.ReadQ();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        _chatService.GagPlayer(_playerId, _gagTime);
        return ValueTask.CompletedTask;
    }
}
