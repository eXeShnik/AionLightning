using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client reads express/system mail. Stub — opcode 0x160.</summary>
public sealed class CM_READ_EXPRESS_MAIL : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // mailId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
