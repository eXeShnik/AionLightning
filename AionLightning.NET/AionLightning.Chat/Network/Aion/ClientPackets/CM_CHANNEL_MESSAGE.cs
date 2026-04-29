using AionLightning.Chat.Network.Aion.ServerPackets;
using AionLightning.Chat.Service;
using AionLightning.Commons.Network;
using System.Text;

namespace AionLightning.Chat.Network.Aion.ClientPackets;

public sealed class CM_CHANNEL_MESSAGE : AionClientPacket
{
    private readonly AionClientConnection _conn;
    private readonly ChatService _chatService;

    private int _channelId;
    private byte[] _content = [];

    public CM_CHANNEL_MESSAGE(AionClientConnection conn, ChatService chatService)
    {
        _conn = conn;
        _chatService = chatService;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadH(); r.ReadC();
        r.ReadD(); r.ReadD(); r.ReadD(); r.ReadD();
        _channelId = r.ReadD();
        r.ReadC();
        int len = r.ReadH() * 2;
        _content = r.ReadB(len);
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var chatClient = _conn.ChatClient;
        if (chatClient is null) return;

        var channel = _chatService.GetChannelById(_channelId);
        if (channel is null) return;

        if (!chatClient.VerifyLastMessage(_chatService.MessageDelaySeconds))
        {
            byte[] throttleMsg = Encoding.Unicode.GetBytes("You can use chat only once every 30 seconds.");
            await _conn.SendAsync(new SM_CHANNEL_MESSAGE(channel.ChannelId, chatClient.ClientId, chatClient.Identifier, throttleMsg), ct);
            return;
        }

        if (chatClient.IsGagged())
        {
            long endMin = (chatClient.GetGagTime() - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 60_000;
            byte[] gagMsg = Encoding.Unicode.GetBytes($"You have been gagged for {endMin} minutes.");
            await _conn.SendAsync(new SM_CHANNEL_MESSAGE(channel.ChannelId, chatClient.ClientId, chatClient.Identifier, gagMsg), ct);
            return;
        }

        await _chatService.BroadcastAsync(channel, chatClient, _content, ct);
    }
}
