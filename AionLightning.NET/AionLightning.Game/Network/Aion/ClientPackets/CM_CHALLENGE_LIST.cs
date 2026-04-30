using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the challenge task list. Stub — opcode 0x18A.</summary>
public sealed class CM_CHALLENGE_LIST : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // action
        r.ReadD(); // taskOwner
        r.ReadC(); // ownerType
        r.ReadD(); // playerId
        r.ReadD(); // dateSince
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
