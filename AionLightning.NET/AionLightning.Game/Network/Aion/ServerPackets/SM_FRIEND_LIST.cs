using AionLightning.Commons.Network;
using AionLightning.Game.Model.Social;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's friend list. Opcode 0x84.</summary>
public sealed class SM_FRIEND_LIST : AionServerPacket
{
    private readonly IReadOnlyList<FriendEntry> _friends;
    private readonly HashSet<int> _onlineIds;

    public SM_FRIEND_LIST(IReadOnlyList<FriendEntry> friends, HashSet<int> onlineIds) : base(0x84)
    {
        _friends   = friends;
        _onlineIds = onlineIds;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)(-_friends.Count));
        foreach (var f in _friends)
        {
            w.WriteD(f.PlayerId);
            w.WriteS(f.Name);
            w.WriteC((byte)f.PlayerClass);
            w.WriteC(f.Level);
            w.WriteC((byte)f.Race);
            w.WriteC(_onlineIds.Contains(f.PlayerId) ? (byte)1 : (byte)0);
            w.WriteC(0); // mutual flag — not tracked in this impl
            w.WriteS(f.Note);
        }
        w.WriteC(0);
    }
}
