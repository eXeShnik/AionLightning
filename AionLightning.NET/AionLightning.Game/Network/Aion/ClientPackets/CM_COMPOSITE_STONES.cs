using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client combines composite stones. Stub — opcode 0x192.</summary>
public sealed class CM_COMPOSITE_STONES : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // combinationToolItemObjectId
        r.ReadD(); // firstItemObjectId
        r.ReadD(); // secondItemObjectId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
