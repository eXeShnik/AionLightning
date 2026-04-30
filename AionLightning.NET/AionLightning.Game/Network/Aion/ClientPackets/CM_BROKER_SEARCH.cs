using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client searches the broker. Stub — opcode 0x15E.</summary>
public sealed class CM_BROKER_SEARCH : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // brokerId
        r.ReadC(); // sortType
        r.ReadH(); // page
        r.ReadH(); // mask
        int count = r.ReadH();
        for (int i = 0; i < count; i++)
            r.ReadD(); // itemId[i]
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
