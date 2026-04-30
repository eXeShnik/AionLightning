using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client reports another player. Stub — opcode 0x19D.</summary>
public sealed class CM_REPORT_PLAYER : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadB(1); // unk
        r.ReadS();  // player name to report
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
