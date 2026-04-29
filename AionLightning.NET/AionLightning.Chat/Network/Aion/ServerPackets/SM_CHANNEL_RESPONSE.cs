using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ServerPackets;

public sealed class SM_CHANNEL_RESPONSE : AionServerPacket
{
    private readonly int _channelId;
    private readonly int _channelIndex;

    public SM_CHANNEL_RESPONSE(int channelId, int channelIndex) : base(0x11)
    {
        _channelId = channelId;
        _channelIndex = channelIndex;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x40);
        w.WriteH((short)_channelIndex);
        w.WriteH(0x00);
        w.WriteH(0x00);
        w.WriteD(_channelId);
    }
}
