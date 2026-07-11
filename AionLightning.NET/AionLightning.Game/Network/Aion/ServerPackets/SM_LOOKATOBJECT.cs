using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Makes a creature visually turn toward its target (Java SM_LOOKATOBJECT). Opcode 0x28.
/// Broadcast when an NPC changes target.
/// </summary>
public sealed class SM_LOOKATOBJECT : AionServerPacket
{
    private readonly int  _objectId;
    private readonly int  _targetObjectId;
    private readonly byte _heading;

    public SM_LOOKATOBJECT(Npc npc) : base(0x28)
    {
        _objectId       = npc.ObjectId;
        _targetObjectId = npc.Target?.ObjectId ?? 0;
        _heading        = (byte)npc.Position.Heading;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteD(_targetObjectId);
        w.WriteC(_heading);
    }
}
