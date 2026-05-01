using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a legion member's updated nickname. Opcode 0x0B.</summary>
public sealed class SM_LEGION_UPDATE_NICKNAME : AionServerPacket
{
    private readonly int    _playerObjId;
    private readonly string _nickname;

    public SM_LEGION_UPDATE_NICKNAME(int playerObjId, string nickname) : base(0x0B)
    {
        _playerObjId = playerObjId;
        _nickname    = nickname;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjId);
        w.WriteS(_nickname);
    }
}
