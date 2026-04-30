using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client shares a quest with the group. Stub — opcode 0x146.</summary>
public sealed class CM_QUEST_SHARE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // questId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
