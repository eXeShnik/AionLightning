using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deposits/withdraws kinah from legion warehouse. Stub — opcode 0x2EE.</summary>
public sealed class CM_LEGION_WH_KINAH : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadQ(); // amount
        r.ReadC(); // operation (deposit/withdraw)
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
