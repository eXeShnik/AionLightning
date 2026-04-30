using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies client of package/shop info count. Opcode 0x10A.</summary>
public sealed class SM_PACKAGE_INFO_NOTIFY : AionServerPacket
{
    private readonly short _count;

    public SM_PACKAGE_INFO_NOTIFY(short count = 0) : base(0x10A) => _count = count;

    public override void Write(ref PacketWriter w) => w.WriteH(_count);
}
