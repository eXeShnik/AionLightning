using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client submits a GM petition. Stub — opcode 0xF6.</summary>
public sealed class CM_PETITION : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int action = r.ReadH();
        if (action == 2)
            r.ReadD(); // unk
        else
            r.ReadS(); // petition data string
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
