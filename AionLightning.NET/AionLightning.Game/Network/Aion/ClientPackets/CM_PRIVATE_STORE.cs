using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client interacts with a private shop. Stub — opcode 0x155.</summary>
public sealed class CM_PRIVATE_STORE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int itemCount = r.ReadH();
        for (int i = 0; i < itemCount; i++)
        {
            r.ReadD(); // itemObjId
            r.ReadD(); // itemId
            r.ReadH(); // count
            r.ReadD(); // price
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
