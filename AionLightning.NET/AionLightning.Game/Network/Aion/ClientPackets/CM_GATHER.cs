using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client gathers a resource node. Stub — opcode 0xD1.</summary>
public sealed class CM_GATHER : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // targetObjectId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
