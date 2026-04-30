using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client modifies the legion emblem. Stub — opcode 0x119.</summary>
public sealed class CM_LEGION_MODIFY_EMBLEM : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
