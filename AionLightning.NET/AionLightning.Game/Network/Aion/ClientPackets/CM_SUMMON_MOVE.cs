using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client moves a summon. Stub — opcode 0x16B.</summary>
public sealed class CM_SUMMON_MOVE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // summon objectId
        r.ReadF(); r.ReadF(); r.ReadF(); // x, y, z
        r.ReadC(); // heading
        byte type = (byte)r.ReadC();

        if ((type & MovementMask.StartMove) != 0)
        {
            // Java comment: vector reads are absent in summon packets when Mouse is not set
            if ((type & MovementMask.Mouse) != 0)
            {
                r.ReadF(); r.ReadF(); r.ReadF(); // x2, y2, z2 (absolute destination)
            }
        }
        if ((type & MovementMask.Glide) != 0)
            r.ReadC(); // glideFlag
        if ((type & MovementMask.Vehicle) != 0)
        {
            r.ReadD(); r.ReadD();               // unk1, unk2
            r.ReadF(); r.ReadF(); r.ReadF();    // vehicleX, vehicleY, vehicleZ
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
