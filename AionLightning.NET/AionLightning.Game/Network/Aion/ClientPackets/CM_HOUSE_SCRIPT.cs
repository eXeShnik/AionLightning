using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sends a house object script command. Stub — opcode 0xFC.</summary>
public sealed class CM_HOUSE_SCRIPT : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // address
        r.ReadC(); // scriptIndex
        int totalSize = r.ReadH();
        if (totalSize > 0)
        {
            int compressedSize = r.ReadD();
            if (compressedSize < 8150)
            {
                r.ReadD(); // uncompressedSize
                r.ReadB(compressedSize);
            }
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
