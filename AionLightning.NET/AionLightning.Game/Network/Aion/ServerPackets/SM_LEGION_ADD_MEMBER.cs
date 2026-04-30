using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Adds or updates a member in the client's legion member list. Opcode 0x6F.
/// Also used to broadcast rank changes to all legion members.
/// </summary>
public sealed class SM_LEGION_ADD_MEMBER : AionServerPacket
{
    private readonly Player      _player;
    private readonly LegionMember _member;

    public SM_LEGION_ADD_MEMBER(Player player, LegionMember member) : base(0x6F)
    {
        _player = player;
        _member = member;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_player.ObjectId);
        w.WriteS(_player.Name);
        w.WriteC((byte)_member.Rank);
        w.WriteC(1);
        w.WriteC((byte)_player.PlayerClass);
        w.WriteC(_player.Level);
        w.WriteD(_player.Position.WorldId);
        w.WriteD(1);
        w.WriteD(0);
        w.WriteS("");
    }
}
