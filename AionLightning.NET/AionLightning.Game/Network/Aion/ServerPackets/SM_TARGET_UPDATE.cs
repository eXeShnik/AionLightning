using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Broadcasts a player's new target to surrounding players. Opcode 0x51.
/// </summary>
public sealed class SM_TARGET_UPDATE : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly int _targetObjectId;

    public SM_TARGET_UPDATE(Player player) : base(0x51)
    {
        _playerObjectId = player.ObjectId;
        _targetObjectId = player.Target?.ObjectId ?? 0;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteD(_targetObjectId);
    }
}
