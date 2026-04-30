using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Social;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Result of a friend list add or remove. Opcode 0x12E.</summary>
public sealed class SM_FRIEND_RESPONSE : AionServerPacket
{
    public enum ResultCode : byte
    {
        Added          = 0,
        AlreadyFriends = 1,
        LimitReached   = 2,
        Blocked        = 3,
        NotFound       = 4,
        Removed        = 5,
    }

    private readonly FriendEntry? _entry;
    private readonly ResultCode   _result;
    private readonly bool         _isOnline;

    public SM_FRIEND_RESPONSE(FriendEntry entry, ResultCode result, bool isOnline) : base(0x12E)
    {
        _entry    = entry;
        _result   = result;
        _isOnline = isOnline;
    }

    public SM_FRIEND_RESPONSE(ResultCode result) : base(0x12E)
        => _result = result;

    public override void Write(ref PacketWriter w)
    {
        if (_entry is not null)
        {
            w.WriteD(_entry.PlayerId);
            w.WriteS(_entry.Name);
            w.WriteC((byte)_entry.PlayerClass);
            w.WriteC(_entry.Level);
            w.WriteC((byte)_entry.Race);
            w.WriteC(_isOnline ? (byte)1 : (byte)0);
            w.WriteC(0); // mutual
            w.WriteS(_entry.Note);
        }
        else
        {
            w.WriteD(0);
            w.WriteS(string.Empty);
            w.WriteC(0);
            w.WriteC(0);
            w.WriteC(0);
            w.WriteC(0);
            w.WriteC(0);
            w.WriteS(string.Empty);
        }
        w.WriteC((byte)_result);
    }
}
