using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Opens the exchange/trade window on the client. Opcode 0x4A.</summary>
public sealed class SM_EXCHANGE_REQUEST : AionServerPacket
{
    private readonly string _partnerName;

    public SM_EXCHANGE_REQUEST(string partnerName) : base(0x4A)
        => _partnerName = partnerName;

    public override void Write(ref PacketWriter w) => w.WriteS(_partnerName);
}
