using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_MACBAN_LIST : AionServerPacket
{
    public SM_MACBAN_LIST() : base(0x09) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0);
    }
}
