using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client charges a chargeable item. Stub — opcode 0x2EC.</summary>
public sealed class CM_CHARGE_ITEM : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // targetNpcObjectId
        r.ReadC(); // chargeLevel
        int count = r.ReadH();
        for (int i = 0; i < count; i++)
            r.ReadD(); // itemId[i]
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
