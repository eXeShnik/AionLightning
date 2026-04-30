using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client searches the group-finder board. Stub — opcode 0x2EF.</summary>
public sealed class CM_FIND_GROUP : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        byte action = (byte)r.ReadC();
        switch (action)
        {
            case 0x01: // delete offer
                r.ReadD(); // playerObjId
                r.ReadD(); // unk
                break;
            case 0x02: // send offer
                r.ReadD(); // playerObjId
                r.ReadH(); // minLevel
                r.ReadH(); // maxLevel
                r.ReadD(); // instanceMaskId
                r.ReadS(); // comment
                break;
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
