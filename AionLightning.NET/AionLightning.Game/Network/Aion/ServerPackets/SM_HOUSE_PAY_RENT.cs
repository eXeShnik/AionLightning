using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_PAY_RENT — acknowledges a successful CM_HOUSE_PAY_RENT
/// payment with the number of weeks just paid.
/// Opcode 0x106 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_HOUSE_PAY_RENT : AionServerPacket
{
    private readonly int _weeksPaid;

    public SM_HOUSE_PAY_RENT(int weeksPaid) : base(0x106)
    {
        _weeksPaid = weeksPaid;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0);
        w.WriteC((byte)_weeksPaid);
    }
}
