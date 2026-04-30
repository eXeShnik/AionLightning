using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client edits house furnishings. Stub — opcode 0x110.</summary>
public sealed class CM_HOUSE_EDIT : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        byte actionId = (byte)r.ReadC();
        // 1=ADD_ITEM, 2=DELETE_ITEM, 3=DESPAWN_OBJECT → D(itemObjectId)
        // 4=SPAWN_OBJECT, 5=MOVE_OBJECT → D+F+F+F+C+H+H+H
        if (actionId == 1 || actionId == 2 || actionId == 3)
            r.ReadD(); // itemObjectId
        else if (actionId == 4 || actionId == 5)
        {
            r.ReadD(); r.ReadF(); r.ReadF(); r.ReadF();
            r.ReadC(); r.ReadH(); r.ReadH(); r.ReadH();
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
