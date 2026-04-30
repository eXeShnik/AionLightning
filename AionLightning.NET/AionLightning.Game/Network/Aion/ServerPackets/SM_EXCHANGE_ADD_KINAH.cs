using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies both trade partners that kinah was offered in the exchange. Opcode 0x4D.</summary>
public sealed class SM_EXCHANGE_ADD_KINAH : AionServerPacket
{
    private readonly byte _action; // 0 = self's side, 1 = partner's side
    private readonly long _amount;

    public SM_EXCHANGE_ADD_KINAH(byte action, long amount) : base(0x4D)
    {
        _action = action;
        _amount = amount;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        w.WriteD((int)_amount);
        w.WriteD(0); // unk
    }
}
