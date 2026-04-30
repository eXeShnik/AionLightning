using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Delivers a chat message to the client. Opcode 0x18.</summary>
public sealed class SM_MESSAGE : AionServerPacket
{
    public enum ChatType
    {
        Normal  = 0x00,
        Shout   = 0x03,
        Whisper = 0x04,
        Group   = 0x05,
        Legion  = 0x0A,
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
            case ChatType.Legion:
                w.WriteS(_senderName);
                w.WriteS(_message);
                break;
        }
    }
}
