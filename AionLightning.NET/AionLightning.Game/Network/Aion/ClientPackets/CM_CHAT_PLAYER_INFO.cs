using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests chat info for a player. Stub — opcode 0xC5.</summary>
public sealed class CM_CHAT_PLAYER_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadS(); // player name
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
