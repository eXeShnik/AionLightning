using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the legion emblem image. Stub — opcode 0xCD.</summary>
public sealed class CM_LEGION_SEND_EMBLEM : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // legionId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
