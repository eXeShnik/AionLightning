using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests legion tabs. Stub — opcode 0x115.</summary>
public sealed class CM_LEGION_TABS : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // legionId
        r.ReadC(); // tabId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
