using AionLightning.Chat.Network.Aion.ServerPackets;
using AionLightning.Chat.Service;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ClientPackets;

public sealed class CM_CHANNEL_REQUEST : AionClientPacket
{
    private readonly AionClientConnection _conn;
    private readonly ChatService _chatService;

    private int _channelIndex;
    private byte[] _channelIdentifier = [];

    public CM_CHANNEL_REQUEST(AionClientConnection conn, ChatService chatService)
    {
        _conn = conn;
        _chatService = chatService;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // 0x40
        r.ReadH(); // 0x00
        _channelIndex = r.ReadH();
        r.ReadB(18);
        int idLen = r.ReadH() * 2;
        _channelIdentifier = r.ReadB(idLen);
        r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var chatClient = _conn.ChatClient;
        if (chatClient is null) return;

        var channel = _chatService.RegisterPlayerWithChannel(chatClient, _channelIdentifier);
        if (channel is not null)
            await _conn.SendAsync(new SM_CHANNEL_RESPONSE(channel.ChannelId, _channelIndex), ct);
    }
}
