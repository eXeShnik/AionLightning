using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client exchanges group-related data. Stub — opcode 0x2ED.</summary>
public sealed class CM_GROUP_DATA_EXCHANGE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        byte action = (byte)r.ReadC();
        if (action == 1)
            r.ReadD(); // dataSize
        else
        {
            r.ReadC(); // groupType
            r.ReadC(); // unk2
            r.ReadD(); // dataSize
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
