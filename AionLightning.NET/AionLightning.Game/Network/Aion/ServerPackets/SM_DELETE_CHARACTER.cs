using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_DELETE_CHARACTER : AionServerPacket
{
    private readonly int _playerObjId;
    private readonly int _deletionTime;

    public SM_DELETE_CHARACTER(int playerObjId, int deletionTime) : base(0xCA)
    {
        _playerObjId = playerObjId;
        _deletionTime = deletionTime;
    }

    public override void Write(ref PacketWriter w)
    {
        if (_playerObjId != 0)
        {
            w.WriteD(0x00);
            w.WriteD(_playerObjId);
            w.WriteD(_deletionTime);
        }
        else
        {
            w.WriteD(0x10);
            w.WriteD(0x00);
            w.WriteD(0x00);
        }
    }
}
