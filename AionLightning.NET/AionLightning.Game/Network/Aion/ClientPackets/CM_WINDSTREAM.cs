using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client interacts with a windstream. Stub — opcode 0x2E4.</summary>
public sealed class CM_WINDSTREAM : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // teleportId
        r.ReadD(); // distance
        r.ReadD(); // state
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
