using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ServerPackets;

public sealed class SM_CHANNEL_MESSAGE : AionServerPacket
{
    private readonly int _channelId;
    private readonly int _senderId;
    private readonly byte[] _senderIdentifier;
    private readonly byte[] _content;

    public SM_CHANNEL_MESSAGE(int channelId, int senderId, byte[] senderIdentifier, byte[] content)
        : base(0x1A)
    {
        _channelId = channelId;
        _senderId = senderId;
        _senderIdentifier = senderIdentifier;
        _content = content;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(_channelId);
        w.WriteD(_senderId);
        w.WriteD(0x00);
        w.WriteC(0x00);
        w.WriteH((short)(_senderIdentifier.Length / 2));
        w.WriteB(_senderIdentifier);
        w.WriteH((short)(_content.Length / 2));
        w.WriteB(_content);
    }
}
