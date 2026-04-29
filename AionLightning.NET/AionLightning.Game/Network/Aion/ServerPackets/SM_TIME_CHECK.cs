using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_TIME_CHECK : AionServerPacket
{
    private readonly int _nanoTime;

    public SM_TIME_CHECK(int nanoTime) : base(0x27)
    {
        _nanoTime = nanoTime;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_nanoTime);
        w.WriteD((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}
