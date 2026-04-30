using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client removes their registered broker listing. Stub — opcode 0x142.</summary>
public sealed class CM_BROKER_CANCEL_REGISTERED : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
