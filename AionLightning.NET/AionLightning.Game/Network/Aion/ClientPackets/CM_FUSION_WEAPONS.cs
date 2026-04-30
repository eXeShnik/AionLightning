using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client fuses two weapons. Stub — opcode 0x16C.</summary>
public sealed class CM_FUSION_WEAPONS : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // unk
        r.ReadD(); // firstItemId
        r.ReadD(); // secondItemId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
