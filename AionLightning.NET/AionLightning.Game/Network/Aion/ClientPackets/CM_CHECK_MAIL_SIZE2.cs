using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client checks express mail attachment size. Stub — opcode 0x1B7.</summary>
public sealed class CM_CHECK_MAIL_SIZE2 : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
