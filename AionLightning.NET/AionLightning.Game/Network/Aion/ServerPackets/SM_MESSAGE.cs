using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Delivers a chat message to the client. Opcode 0x18.</summary>
public sealed class SM_MESSAGE : AionServerPacket
{
    public enum ChatType
    {
        Normal       = 0x00,
        Shout        = 0x03,
        Whisper      = 0x04,
        Group        = 0x05,
        Alliance     = 0x06,
        GroupLeader  = 0x07,
        Legion       = 0x0A,
        Trade        = 0x0E, // CH1 — server-wide trade channel
        Lfg          = 0x0F, // CH2 — server-wide LFG channel
        Region       = 0x10, // CH3 — server-wide region channel
        Command      = 0x18, // Yellow — used for GM announcements
    }

    private readonly ChatType _chatType;
    private readonly int      _senderObjectId;
    private readonly string   _senderName;
    private readonly string   _message;
    private readonly Race     _senderRace;
    private readonly float    _x, _y, _z;

    public SM_MESSAGE(Player sender, string message, ChatType chatType) : base(0x18)
    {
        _chatType       = chatType;
        _senderObjectId = sender.ObjectId;
        _senderName     = sender.Name;
        _message        = message;
        _senderRace     = sender.Race;
        _x              = sender.Position.X;
        _y              = sender.Position.Y;
        _z              = sender.Position.Z;
    }

    /// <summary>
    /// Sender-less overload for server-originated broadcasts with no live player behind them (e.g.
    /// AnnouncementService's scheduled announcements — Java's "manual creation" constructor took a raw
    /// <c>senderObjectId</c> instead of a <see cref="Player"/>, and likewise left position at its default).
    /// <see cref="_senderRace"/> is never read by <see cref="Write"/>, so <see cref="Race.PC_ALL"/> is just
    /// a harmless placeholder; <see cref="ChatType.Shout"/> still reads <c>x</c>/<c>y</c>/<c>z</c>, but
    /// Java's own equivalent constructor leaves them at 0 too, so zeroing here matches faithfully.
    /// </summary>
    public SM_MESSAGE(int senderObjectId, string senderName, string message, ChatType chatType) : base(0x18)
    {
        _chatType       = chatType;
        _senderObjectId = senderObjectId;
        _senderName     = senderName;
        _message        = message;
        _senderRace     = Race.PC_ALL;
        _x = _y = _z    = 0;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)_chatType);
        w.WriteC(0); // race restriction: 0 = all can read
        w.WriteD(_senderObjectId);

        switch (_chatType)
        {
            case ChatType.Normal:
                w.WriteH(0);
                w.WriteS(_message);
                break;

            case ChatType.Shout:
                w.WriteS(_senderName);
                w.WriteS(_message);
                w.WriteF(_x);
                w.WriteF(_y);
                w.WriteF(_z);
                break;

            case ChatType.Whisper:
            case ChatType.Group:
            case ChatType.GroupLeader:
            case ChatType.Alliance:
            case ChatType.Legion:
            case ChatType.Trade:
            case ChatType.Lfg:
            case ChatType.Region:
            case ChatType.Command:
                w.WriteS(_senderName);
                w.WriteS(_message);
                break;
        }
    }
}
