using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client places/moves a house decoration. Stub — opcode 0x2E9.</summary>
public sealed class CM_HOUSE_DECORATE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // objectId
        r.ReadD(); // templateId
        r.ReadH(); // lineNr
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
