using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests legion emblem metadata. Stub — opcode 0xD2.</summary>
public sealed class CM_LEGION_SEND_EMBLEM_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // legionId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
