using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client collects broker settlement kinah. Stub — opcode 0x140.</summary>
public sealed class CM_BROKER_SETTLE_ACCOUNT : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
