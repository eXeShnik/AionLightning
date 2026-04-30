using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests broker settlement list. Stub — opcode 0x143.</summary>
public sealed class CM_BROKER_SETTLE_LIST : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // npcId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
