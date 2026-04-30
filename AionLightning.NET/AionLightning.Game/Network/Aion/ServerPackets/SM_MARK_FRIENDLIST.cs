using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Signals completion of friend list delivery. Opcode 0x117.</summary>
public sealed class SM_MARK_FRIENDLIST : AionServerPacket
{
    private readonly int _playerObjectId;

    public SM_MARK_FRIENDLIST(int playerObjectId) : base(0x117)
        => _playerObjectId = playerObjectId;

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteC(1);
        w.WriteH(0);
    }
}
