using System.Collections.Concurrent;
using AionLightning.Chat.Network.Aion;

namespace AionLightning.Chat.Model;

public sealed class ChatClient
{
    public int ClientId { get; }
    public byte[] Token { get; }
    public string RealName { get; }
    public byte[] Identifier { get; set; } = [];
    public AionClientConnection? ChannelHandler { get; set; }

    private readonly ConcurrentDictionary<ChannelType, Channel> _channels = new();
    private long _gagTime;
    private long _lastMessage;

    public ChatClient(int clientId, byte[] token, string realName)
    {
        ClientId = clientId;
        Token = token;
        RealName = realName;
    }

    public void AddChannel(Channel channel) => _channels[channel.ChannelType] = channel;
    public bool IsInChannel(Channel channel) => _channels.ContainsKey(channel.ChannelType);

    public bool VerifyLastMessage(int delaySeconds)
    {
        if (delaySeconds == 0) return true;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long prev = Interlocked.Exchange(ref _lastMessage, now);
        if (prev == 0) return true;
        return (now - prev) >= delaySeconds * 1000L;
    }

    public bool IsGagged()
    {
        long t = Volatile.Read(ref _gagTime);
        return t != 0 && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < t;
    }

    public void SetGagTime(long gagTimeMs) => Volatile.Write(ref _gagTime, gagTimeMs);
    public long GetGagTime() => Volatile.Read(ref _gagTime);
}
