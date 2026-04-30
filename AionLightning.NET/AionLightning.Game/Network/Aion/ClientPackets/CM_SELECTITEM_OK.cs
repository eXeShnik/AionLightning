using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client confirms item selection from a dialog. Stub — opcode 0x18E.</summary>
public sealed class CM_SELECTITEM_OK : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // uniqueItemId
        r.ReadD(); // unk
        r.ReadC(); // index (which reward to pick)
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
