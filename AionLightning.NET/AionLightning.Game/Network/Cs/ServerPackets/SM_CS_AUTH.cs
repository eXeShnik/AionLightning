using System.Net;
using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Cs.ServerPackets;

public sealed class SM_CS_AUTH : AionServerPacket
{
    private readonly byte _gsId;
    private readonly string _password;

    public SM_CS_AUTH(int gsId, string password) : base(0x00)
    {
        _gsId = (byte)gsId;
        _password = password;
    }

    public override void Write(ref PacketWriter w)
    {
        byte[] ip = IPAddress.Loopback.GetAddressBytes();
        w.WriteC(_gsId);
        w.WriteC((byte)ip.Length);
        w.WriteB(ip);
        w.WriteS(_password);
    }
}
