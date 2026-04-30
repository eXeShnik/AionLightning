using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests a duel. Stub — opcode 0x130.</summary>
public sealed class CM_DUEL_REQUEST : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // targetObjectId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
