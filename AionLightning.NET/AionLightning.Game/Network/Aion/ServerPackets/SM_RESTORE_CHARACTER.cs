using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Response to CM_RESTORE_CHARACTER. Opcode 0xCB.
/// 0x00 = success, 0x10 = failure.
/// </summary>
public sealed class SM_RESTORE_CHARACTER : AionServerPacket
{
    private readonly int _playerObjId;
    private readonly bool _success;

    public SM_RESTORE_CHARACTER(int playerObjId, bool success) : base(0xCB)
    {
        _playerObjId = playerObjId;
        _success     = success;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_success ? 0x00 : 0x10);
        w.WriteD(_playerObjId);
    }
}
