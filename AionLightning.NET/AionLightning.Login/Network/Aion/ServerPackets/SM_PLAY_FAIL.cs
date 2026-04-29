using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_PLAY_FAIL : AionServerPacket
{
    private readonly AionAuthResponse _response;

    public SM_PLAY_FAIL(AionAuthResponse response) : base(0x06)
    {
        _response = response;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD((int)_response);
    }
}
