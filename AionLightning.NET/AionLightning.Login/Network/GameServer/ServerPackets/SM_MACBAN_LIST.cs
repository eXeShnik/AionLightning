using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_MACBAN_LIST : AionServerPacket
{
    public SM_MACBAN_LIST() : base(0x09) { }

    public override void Write(ref PacketWriter w)
    {
        // TODO M2: serialize banned MAC list from BannedMacManager
        w.WriteD(0);
    }
}
