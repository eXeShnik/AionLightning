using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client uploads legion emblem info. Stub — opcode 0x162.</summary>
public sealed class CM_LEGION_UPLOAD_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // legionId
        r.ReadC(); // red
        r.ReadC(); // green
        r.ReadC(); // blue
        r.ReadC(); // alpha
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
