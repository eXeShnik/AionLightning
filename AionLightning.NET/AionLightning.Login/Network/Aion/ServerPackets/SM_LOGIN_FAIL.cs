using AionLightning.Commons.Network;
using AionLightning.Login.Network.Aion;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_LOGIN_FAIL : AionServerPacket
{
    private readonly AionAuthResponse _response;

    public SM_LOGIN_FAIL(AionAuthResponse response) : base(0x01)
    {
        _response = response;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_response.GetMessageId());
    }
}
