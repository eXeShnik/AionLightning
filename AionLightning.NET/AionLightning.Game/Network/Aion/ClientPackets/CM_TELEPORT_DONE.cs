using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Sent by client after the new map finishes loading post-teleport.
/// In Java this only triggers SM_PLAYER_SPAWN for personal instances;
/// for open-world maps no server response is needed.
/// </summary>
public sealed class CM_TELEPORT_DONE : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
