using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Sent by client when crossing a sub-zone boundary. Reads one byte (unk), no response needed.
/// </summary>
public sealed class CM_SUBZONE_CHANGE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadC();
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
