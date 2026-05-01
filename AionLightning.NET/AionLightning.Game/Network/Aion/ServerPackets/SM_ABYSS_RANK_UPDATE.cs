using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Broadcasts a player's new abyss rank to zone peers. Opcode 0x88.
/// Action 0 = rank changed; the new rank ID is written after the object ID.
/// </summary>
public sealed class SM_ABYSS_RANK_UPDATE : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly int _rankId;

    public SM_ABYSS_RANK_UPDATE(int playerObjectId, int rankId) : base(0x88)
    {
        _playerObjectId = playerObjectId;
        _rankId         = rankId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0);               // action = 0 (rank change)
        w.WriteD(_playerObjectId);
        w.WriteD(_rankId);
    }
}
