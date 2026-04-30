using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Shows an NPC location on the client minimap. Opcode 0x59.</summary>
public sealed class SM_SHOW_NPC_ON_MAP : AionServerPacket
{
    private readonly int   _npcId;
    private readonly int   _worldId;
    private readonly float _x, _y, _z;

    public SM_SHOW_NPC_ON_MAP(int npcId, int worldId, float x, float y, float z) : base(0x59)
    {
        _npcId   = npcId;
        _worldId = worldId;
        _x       = x;
        _y       = y;
        _z       = z;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_npcId);
        w.WriteD(_worldId);
        w.WriteD(_worldId); // repeated — matches Java original
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
    }
}
