namespace AionLightning.Commons.Network;

public sealed class PacketFormatException : InvalidOperationException
{
    public PacketFormatException(string message, Exception? inner = null)
        : base(message, inner) { }
}
