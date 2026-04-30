using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Unknown/unidentified client packet. Stub — opcode 0x10F.</summary>
public sealed class CM_UNK : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); r.ReadC(); r.ReadC(); r.ReadC(); r.ReadC();
        r.ReadD(); r.ReadD(); r.ReadD(); r.ReadD(); r.ReadD(); r.ReadD();
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
