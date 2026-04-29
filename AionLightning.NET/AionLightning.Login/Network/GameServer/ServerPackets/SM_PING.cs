using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_PING : AionServerPacket
{
    public SM_PING() : base(0x0B) { }

    public override void Write(ref PacketWriter w) { }
}
