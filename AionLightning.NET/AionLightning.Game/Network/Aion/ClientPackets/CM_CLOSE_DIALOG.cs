using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client closes an NPC dialog window. Opcode 0x117.</summary>
public sealed class CM_CLOSE_DIALOG : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD();
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
