using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_AUTH_GG : AionServerPacket
{
    private readonly int _sessionId;

    public SM_AUTH_GG(int sessionId) : base(0x0B)
    {
        _sessionId = sessionId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_sessionId);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteZero(0x19);
    }
}
