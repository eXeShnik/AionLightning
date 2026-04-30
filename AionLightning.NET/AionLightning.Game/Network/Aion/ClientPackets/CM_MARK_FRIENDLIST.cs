using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client marks friends visible/hidden in the list. Stub — opcode 0x10C.</summary>
public sealed class CM_MARK_FRIENDLIST : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
