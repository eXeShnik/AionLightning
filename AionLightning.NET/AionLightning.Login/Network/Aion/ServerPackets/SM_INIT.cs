using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_INIT : AionServerPacket
{
    private readonly int _sessionId;
    private readonly byte[] _publicRsaKey;
    private readonly byte[] _blowfishKey;

    public SM_INIT(int sessionId, byte[] publicRsaKey, byte[] blowfishKey) : base(0x00)
    {
        _sessionId = sessionId;
        _publicRsaKey = publicRsaKey;
        _blowfishKey = blowfishKey;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_sessionId);
        w.WriteD(0x0000c621);
        w.WriteB(_publicRsaKey);   // 128 bytes scrambled RSA modulus
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteB(_blowfishKey);    // 16 bytes session blowfish key
        w.WriteD(197635);
        w.WriteD(2097152);
    }
}
