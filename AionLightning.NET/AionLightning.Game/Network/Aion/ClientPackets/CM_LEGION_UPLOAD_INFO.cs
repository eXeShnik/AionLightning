using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client uploads legion emblem info. Stub — opcode 0x162.</summary>
public sealed class CM_LEGION_UPLOAD_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
