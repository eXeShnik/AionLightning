using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests instance cooldown info. Stub — opcode 0x182.</summary>
public sealed class CM_INSTANCE_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // unk1
        r.ReadC(); // unk2 (team?)
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
