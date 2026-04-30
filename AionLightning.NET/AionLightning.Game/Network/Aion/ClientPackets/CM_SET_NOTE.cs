using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sets the character note/memo. Stub — opcode 0x118.</summary>
public sealed class CM_SET_NOTE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadS(); // note text
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
