using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the broker item list. Stub — opcode 0x159.</summary>
public sealed class CM_BROKER_LIST : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // brokerId (npc objectId)
        r.ReadC(); // sortType (1=name,2=level,4=totalPrice,6=unitPrice)
        r.ReadH(); // page
        r.ReadH(); // listMask
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
