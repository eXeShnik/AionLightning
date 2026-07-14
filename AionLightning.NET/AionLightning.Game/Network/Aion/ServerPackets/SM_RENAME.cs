using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_RENAME — broadcast to every online player when a character is
/// renamed. Opcode 0x58 (4.5-era packet table).
/// </summary>
public sealed class SM_RENAME : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly string _oldName;
    private readonly string _newName;

    public SM_RENAME(int playerObjectId, string oldName, string newName) : base(0x58)
    {
        _playerObjectId = playerObjectId;
        _oldName = oldName;
        _newName = newName;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0); // unk
        w.WriteD(0); // unk - 0 or 3
        w.WriteD(_playerObjectId);
        w.WriteS(_oldName);
        w.WriteS(_newName);
    }
}
