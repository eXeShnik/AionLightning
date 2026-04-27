using AionLightning.Commons.Network;
using AionLightning.Login.Network.GameServer;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_GS_AUTH_RESPONSE : AionServerPacket
{
    private readonly GsAuthResponse _response;
    private readonly byte _serverId;

    public SM_GS_AUTH_RESPONSE(GsAuthResponse response, byte serverId = 0) : base(0x00)
    {
        _response = response;
        _serverId = serverId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0);
        w.WriteC((byte)_response);
        if (_response == GsAuthResponse.AUTHED)
            w.WriteC(_serverId);
    }
}
