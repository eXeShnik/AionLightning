using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AionLightning.Chat.Configs.Options;
using AionLightning.Chat.Model;
using AionLightning.Chat.Network.Aion;
using AionLightning.Chat.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Chat.Service;

public sealed class ChatService
{
    private readonly ILogger<ChatService> _log;
    private readonly ChatAuthOptions _opts;
    private readonly ChatChannels _channels;
    private readonly ConcurrentDictionary<int, ChatClient> _players = new();

    public ChatService(ILogger<ChatService> log, IOptions<ChatAuthOptions> opts, ChatChannels channels)
    {
        _log = log;
        _opts = opts.Value;
        _channels = channels;
    }

    public ChatClient RegisterPlayer(int playerId, string playerLogin, string nick)
    {
        byte[] accountToken = SHA256.HashData(Encoding.UTF8.GetBytes(playerLogin));
        byte[] token = GenerateToken(accountToken);
        var client = new ChatClient(playerId, token, nick);
        _players[playerId] = client;
        return client;
    }

    public async ValueTask RegisterPlayerConnectionAsync(
        int playerId, byte[] token, byte[] identifierBytes,
        AionClientConnection handler, string realName, CancellationToken ct)
    {
        if (!_players.TryGetValue(playerId, out var chatClient))
        {
            _log.LogWarning("Player {Id} not registered on ChatServer", playerId);
            return;
        }

        if (!token.AsSpan().SequenceEqual(chatClient.Token.AsSpan()))
        {
            _log.LogWarning("Player {Id} sent invalid token — disconnecting", playerId);
            await handler.DisposeAsync();
            return;
        }

        string identity = Encoding.Unicode.GetString(identifierBytes);
        string full = chatClient.RealName + "@" + identity;
        chatClient.Identifier = Encoding.Unicode.GetBytes(full);
        chatClient.ChannelHandler = handler;
        handler.SetChatClient(chatClient);

        await handler.SendAsync(new SM_PLAYER_AUTH_RESPONSE(), ct);
    }

    public Channel? RegisterPlayerWithChannel(ChatClient chatClient, byte[] channelIdentifier)
    {
        var channel = _channels.GetByIdentifierBytes(channelIdentifier);
        if (channel == null) return null;

        if (channel.ChannelType == ChannelType.GROUP && chatClient.IsInChannel(channel))
            return null;

        chatClient.AddChannel(channel);
        return channel;
    }

    public async ValueTask PlayerLogoutAsync(int playerId)
    {
        if (!_players.TryRemove(playerId, out var chatClient))
            return;

        _log.LogInformation("Player {Id} logged out from ChatServer", playerId);

        if (chatClient.ChannelHandler != null)
            await chatClient.ChannelHandler.DisposeAsync();
    }

    public void GagPlayer(int playerId, long gagTimeMs)
    {
        if (_players.TryGetValue(playerId, out var client))
            client.SetGagTime(gagTimeMs);
    }

    public async ValueTask BroadcastAsync(Channel channel, ChatClient sender, byte[] content, CancellationToken ct)
    {
        var msg = new SM_CHANNEL_MESSAGE(channel.ChannelId, sender.ClientId, sender.Identifier, content);
        foreach (var client in _players.Values)
        {
            if (!client.IsInChannel(channel)) continue;
            if (client.ChannelHandler is { } handler)
                await handler.SendAsync(msg, ct);
        }
    }

    public ChatClient? GetPlayer(int playerId)
        => _players.TryGetValue(playerId, out var c) ? c : null;

    public Model.Channel? GetChannelById(int channelId) => _channels.GetById(channelId);

    public int MessageDelaySeconds => _opts.MessageDelaySeconds;

    private static byte[] GenerateToken(byte[] accountToken)
    {
        byte[] dynamic = RandomNumberGenerator.GetBytes(16);
        byte[] token = new byte[48];
        dynamic.CopyTo(token, 0);
        accountToken.AsSpan(0, 32).CopyTo(token.AsSpan(16));
        return token;
    }
}
