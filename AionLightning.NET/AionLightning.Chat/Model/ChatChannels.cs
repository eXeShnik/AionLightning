using System.Collections.Concurrent;
using System.Text;

namespace AionLightning.Chat.Model;

/// <summary>
/// On-demand channel registry: channels are created on first request, keyed by UTF-16LE identifier bytes.
/// Channel type is inferred from the identifier prefix (public_ / trade_ / partyFind_ / job_ / User_).
/// </summary>
public sealed class ChatChannels
{
    private readonly ConcurrentDictionary<string, Channel> _byIdentifier = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, Channel> _byId = new();

    public Channel? GetByIdentifierBytes(byte[] identifierBytes)
    {
        string key = Encoding.Unicode.GetString(identifierBytes);
        if (_byIdentifier.TryGetValue(key, out var existing))
            return existing;

        var type = InferType(key);
        var channel = new Channel(type, key);
        channel = _byIdentifier.GetOrAdd(key, channel);
        _byId[channel.ChannelId] = channel;
        return channel;
    }

    public Channel? GetById(int channelId)
        => _byId.TryGetValue(channelId, out var ch) ? ch : null;

    private static ChannelType InferType(string identifier)
    {
        var name = identifier.TrimStart('@');
        if (name.StartsWith("public_", StringComparison.Ordinal))  return ChannelType.PUBLIC;
        if (name.StartsWith("trade_", StringComparison.Ordinal))   return ChannelType.TRADE;
        if (name.StartsWith("partyFind_", StringComparison.Ordinal)) return ChannelType.GROUP;
        if (name.StartsWith("job_", StringComparison.Ordinal))     return ChannelType.JOB;
        if (name.StartsWith("User_", StringComparison.Ordinal))    return ChannelType.LANG;
        return ChannelType.PUBLIC;
    }
}
