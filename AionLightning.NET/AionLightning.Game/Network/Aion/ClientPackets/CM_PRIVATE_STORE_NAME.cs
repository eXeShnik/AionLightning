using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sets the private shop name. Stub — opcode 0x15A.</summary>
public sealed class CM_PRIVATE_STORE_NAME : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadS(); // shop name
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
