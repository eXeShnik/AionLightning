using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client issues a summon command. Stub — opcode 0x15B.</summary>
public sealed class CM_SUMMON_COMMAND : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // mode
        r.ReadD(); // unk1
        r.ReadD(); // unk2
        r.ReadD(); // targetObjId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
