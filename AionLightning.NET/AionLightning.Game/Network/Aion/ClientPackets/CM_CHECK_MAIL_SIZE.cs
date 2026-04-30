using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client checks outgoing mail attachment size. Stub — opcode 0x127.</summary>
public sealed class CM_CHECK_MAIL_SIZE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadC(); // mailSize
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
