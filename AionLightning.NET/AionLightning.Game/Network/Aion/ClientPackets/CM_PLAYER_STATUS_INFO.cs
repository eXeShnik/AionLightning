using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests another player's HP/MP status. Stub — opcode 0x122.</summary>
public sealed class CM_PLAYER_STATUS_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // targetObjectId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
