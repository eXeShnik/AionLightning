using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_CHARACTER_SELECT : AionServerPacket
{
    private readonly byte _type;

    public SM_CHARACTER_SELECT(byte type) : base(0xB1) => _type = type;

    public override void Write(ref PacketWriter w) => w.WriteC(_type);
}
