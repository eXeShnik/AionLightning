using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client reads express/system mail. Stub — opcode 0x160.</summary>
public sealed class CM_READ_EXPRESS_MAIL : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadC(); // action (0=close, 1=open postman)
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
