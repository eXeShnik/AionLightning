using System.Net;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Gs.ServerPackets;

public sealed class SM_GS_AUTH_RESPONSE : AionServerPacket
{
    private readonly byte _responseId;
    private readonly byte[] _ipBytes;
    private readonly int _port;

    public SM_GS_AUTH_RESPONSE(byte responseId, string clientBindAddress, int clientPort)
        : base(0x00)
    {
        _responseId = responseId;
        // Always advertise 127.0.0.1 when bind is 0.0.0.0, client must reach Chat on loopback or configured IP
        var addr = clientBindAddress == "0.0.0.0" ? IPAddress.Loopback : IPAddress.Parse(clientBindAddress);
        _ipBytes = addr.GetAddressBytes();
        _port = clientPort;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_responseId);
        w.WriteB(_ipBytes);
        w.WriteH((short)_port);
    }
}
