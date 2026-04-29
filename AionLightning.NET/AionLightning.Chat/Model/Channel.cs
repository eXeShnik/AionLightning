using System.Text;

namespace AionLightning.Chat.Model;

public sealed class Channel
{
    private static int _nextId = 1;

    public ChannelType ChannelType { get; }
    public string Identifier { get; }
    public byte[] IdentifierBytes { get; }
    public int ChannelId { get; }

    public Channel(ChannelType type, string identifier)
    {
        ChannelType = type;
        Identifier = identifier;
        IdentifierBytes = Encoding.Unicode.GetBytes(identifier);
        ChannelId = Interlocked.Increment(ref _nextId);
    }
}
