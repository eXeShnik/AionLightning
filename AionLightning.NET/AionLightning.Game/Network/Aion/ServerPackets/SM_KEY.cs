using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_KEY : AionServerPacket
{
    private readonly int _falseKey;

    public SM_KEY(int falseKey) : base(0x48)
    {
        _falseKey = falseKey;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_falseKey);
    }
}
