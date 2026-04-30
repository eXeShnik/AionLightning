using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client searches for a player by name. Stub — opcode 0x17D.</summary>
public sealed class CM_PLAYER_SEARCH : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadS(); // search name
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
