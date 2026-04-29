using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_MAY_LOGIN_INTO_GAME : AionServerPacket
{
    public SM_MAY_LOGIN_INTO_GAME() : base(0x89) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0x00); // 0 = allowed
    }
}
