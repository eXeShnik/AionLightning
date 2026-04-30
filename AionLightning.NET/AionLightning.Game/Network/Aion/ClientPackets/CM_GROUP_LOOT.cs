using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client rolls/passes on group loot. Stub — opcode 0x19A.</summary>
public sealed class CM_GROUP_LOOT : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
