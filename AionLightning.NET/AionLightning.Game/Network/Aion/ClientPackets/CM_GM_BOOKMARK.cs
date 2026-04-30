using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sets a GM bookmark location. Stub — opcode 0x11E.</summary>
public sealed class CM_GM_BOOKMARK : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadS(); // "command playerName" string
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
