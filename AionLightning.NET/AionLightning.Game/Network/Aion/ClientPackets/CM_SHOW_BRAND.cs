using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client shows a group target brand marker. Stub — opcode 0x197.</summary>
public sealed class CM_SHOW_BRAND : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // action
        r.ReadD(); // brandId
        r.ReadD(); // targetObjectId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
