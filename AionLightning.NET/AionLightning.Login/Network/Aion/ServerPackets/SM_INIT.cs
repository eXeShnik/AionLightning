using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Ncrypt;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_INIT : AionServerPacket
{
    private readonly int _sessionId;
    private readonly byte[] _publicRsaKey;
    private readonly byte[] _blowfishKey;

    public SM_INIT(LoginConnection client) : base(0x00)
    {
        _sessionId = client.SessionId;
        _publicRsaKey = client.RsaKeyPair.PublicKey;
        _blowfishKey = new byte[16];
        Random.Shared.NextBytes(_blowfishKey);
        // TODO M2: client.Crypt.SetKey(_blowfishKey);
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_sessionId);
        w.WriteD(0x0000c621);
        w.WriteB(_publicRsaKey);
        w.WriteD(0x01);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteB(_blowfishKey);
        w.WriteC(0x00);
    }
}
