using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Per-member group state packet. Opcode 0x6C.
/// Wire format matches Java SM_GROUP_MEMBER_INFO.writeImpl() exactly.
/// </summary>
public sealed class SM_GROUP_MEMBER_INFO : AionServerPacket
{
    /// <summary>GroupEvent IDs from Java GroupEvent enum.</summary>
    public enum GroupEvent : byte
    {
        Leave        = 0x00,
        Movement     = 0x01,
        Disconnected = 0x03,
        Join         = 0x05, // new member — sent to self on join
        EnterOffline = 0x07,
        Update       = 0x09, // HP/MP stat update
        Enter        = 0x13, // existing member info exchange on group join
    }

    private readonly int        _groupId;
    private readonly Player     _member;
    private readonly GroupEvent _event;

    public SM_GROUP_MEMBER_INFO(int groupId, Player member, GroupEvent evt) : base(0x6C)
    {
        _groupId = groupId;
        _member  = member;
        _event   = evt;
    }

    public override void Write(ref PacketWriter w)
    {
        var p = _member;

        w.WriteD(_groupId);
        w.WriteD(p.ObjectId);

        // HP, MP, Flight — always written as DWORDs (Java uses int, not short)
        w.WriteD(p.MaxHp);
        w.WriteD(p.CurrentHp);
        w.WriteD(p.MaxMp);
        w.WriteD(p.CurrentMp);
        w.WriteD(0); // maxFp (flight time — not implemented)
        w.WriteD(0); // currentFp

        w.WriteD(0); // unk (added in 3.5)
        w.WriteD(p.Position.WorldId);
        w.WriteD(p.Position.WorldId); // duplicated in Java
        w.WriteF(p.Position.X);
        w.WriteF(p.Position.Y);
        w.WriteF(p.Position.Z);

        w.WriteC((byte)p.PlayerClass);
        w.WriteC((byte)p.Gender);
        w.WriteC(p.Level);

        w.WriteC((byte)_event);
        w.WriteH(1);   // channel (1 = online)
        w.WriteC(0);   // mentor flag

        switch (_event)
        {
            case GroupEvent.Movement:
            case GroupEvent.Disconnected:
                // no tail
                break;
            case GroupEvent.Leave:
                w.WriteH(0);
                w.WriteC(0);
                break;
            case GroupEvent.Join:
            case GroupEvent.EnterOffline:
                w.WriteS(p.Name);
                break;
            default: // Enter (0x13), Update (0x09)
                w.WriteS(p.Name);
                w.WriteD(0);
                break;
        }
    }
}
