using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

// Client sends its MAC address on connect — no response needed.
public sealed class CM_MAC_ADDRESS : AionClientPacket
{
    public override void Read(ref PacketReader r) { /* ignored */ }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
