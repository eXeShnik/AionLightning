using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client exchanges group-related data. Stub — opcode 0x2ED.</summary>
public sealed class CM_GROUP_DATA_EXCHANGE : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
