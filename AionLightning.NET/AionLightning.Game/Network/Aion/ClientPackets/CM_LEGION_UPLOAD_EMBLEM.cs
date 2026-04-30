using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client uploads legion emblem image data. Stub — opcode 0x163.</summary>
public sealed class CM_LEGION_UPLOAD_EMBLEM : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int size = r.ReadD();
        r.ReadB(size); // emblem image data
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
