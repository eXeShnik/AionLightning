using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client buys in the trade-in-trade store. Stub — opcode 0x13A.</summary>
public sealed class CM_BUY_TRADE_IN_TRADE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // sellerObjectId
        r.ReadD(); // itemId
        r.ReadD(); // count
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
