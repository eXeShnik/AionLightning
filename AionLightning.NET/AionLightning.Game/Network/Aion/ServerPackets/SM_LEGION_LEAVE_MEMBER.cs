using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies legion members that a player has left the legion. Opcode 0x70.</summary>
public sealed class SM_LEGION_LEAVE_MEMBER : AionServerPacket
{
    private readonly int    _playerObjectId;
    private readonly string _name;
    private readonly string _name1;
    private readonly int    _msgId;

    public SM_LEGION_LEAVE_MEMBER(int playerObjectId, string name, string name1 = "", int msgId = 0) : base(0x70)
    {
        _playerObjectId = playerObjectId;
        _name           = name;
        _name1          = name1;
        _msgId          = msgId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteC(0);
        w.WriteD(0);
        w.WriteD(_msgId);
        w.WriteS(_name);
        w.WriteS(_name1);
    }
}
