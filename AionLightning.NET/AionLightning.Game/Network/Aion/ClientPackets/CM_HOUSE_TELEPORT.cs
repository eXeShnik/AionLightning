using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client teleports to their house. Stub — opcode 0x1BC.</summary>
public sealed class CM_HOUSE_TELEPORT : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // actionId
        r.ReadD(); // playerId1
        r.ReadD(); // playerId2
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
