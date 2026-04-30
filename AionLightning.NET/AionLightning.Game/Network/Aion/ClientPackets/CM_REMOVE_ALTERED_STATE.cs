using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client removes a buff/debuff state. Stub — opcode 0xE1.</summary>
public sealed class CM_REMOVE_ALTERED_STATE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadH(); // skillId (short)
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
