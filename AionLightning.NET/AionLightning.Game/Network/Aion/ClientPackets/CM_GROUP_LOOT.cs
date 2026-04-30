using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client rolls/passes on group loot. Stub — opcode 0x19A.</summary>
public sealed class CM_GROUP_LOOT : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // groupId
        r.ReadD(); // index
        r.ReadD(); // unk1
        r.ReadD(); // itemId
        r.ReadC(); // unk2
        r.ReadC(); // unk3 (3.0)
        r.ReadC(); // unk4 (3.5)
        r.ReadC(); // unk5 (4.6)
        r.ReadD(); // npcId
        r.ReadC(); // distributionId (2=Roll, 3=Bid)
        r.ReadD(); // roll (0=never, 1=rolled)
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
