using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client pays house rent. Stub — opcode 0x1BD.</summary>
public sealed class CM_HOUSE_PAY_RENT : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadC(); // weekCount
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
