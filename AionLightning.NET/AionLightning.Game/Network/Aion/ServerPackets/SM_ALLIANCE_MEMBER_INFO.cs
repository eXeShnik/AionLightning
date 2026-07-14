using AionLightning.Commons.Network;
using AionLightning.Game.Model.Alliance;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Per-member alliance state packet. Opcode 0xF6 (Java 4.5 table value — TODO: verify opcode vs live
/// 4.6 client). Wire format matches Java <c>SM_ALLIANCE_MEMBER_INFO.writeImpl()</c>, except the
/// abnormal-effects list is always written empty — Java lists the player's active buffs/debuffs here;
/// <c>SM_GROUP_MEMBER_INFO</c> makes the same simplification for the party equivalent in this port —
/// and every member is treated as online (alliance membership always wraps a live connected
/// <see cref="Model.Player"/>, mirroring how <c>PlayerGroup</c>/<c>SM_GROUP_MEMBER_INFO</c> do not model
/// an offline branch either).
/// </summary>
public sealed class SM_ALLIANCE_MEMBER_INFO : AionServerPacket
{
    /// <summary>Semantic event kinds (Java <c>PlayerAllianceEvent</c>) — several share the same wire id.</summary>
    public enum AllianceEvent
    {
        Leave,
        LeaveTimeout,
        Banned,
        Movement,
        Disconnected,
        Join,
        EnterOffline,
        Update,
        Reconnect,
        AppointViceCaptain,
        DemoteViceCaptain,
        AppointCaptain,
        Enter,
        MemberGroupChange,
    }

    private readonly PlayerAllianceMember _member;
    private readonly AllianceEvent _event;

    public SM_ALLIANCE_MEMBER_INFO(PlayerAllianceMember member, AllianceEvent evt) : base(0xF6)
    {
        _member = member;
        _event  = evt;
    }

    /// <summary>Java <c>PlayerAllianceEvent.getId()</c> — the wire-level event byte, not the C# enum ordinal.</summary>
    private static byte WireId(AllianceEvent e) => e switch
    {
        AllianceEvent.Leave or AllianceEvent.LeaveTimeout or AllianceEvent.Banned => 0,
        AllianceEvent.Movement     => 1,
        AllianceEvent.Disconnected => 3,
        AllianceEvent.Join or AllianceEvent.MemberGroupChange => 5,
        AllianceEvent.EnterOffline => 7,
        _ => 13, // Update, Reconnect, Enter, AppointViceCaptain, DemoteViceCaptain, AppointCaptain
    };

    public override void Write(ref PacketWriter w)
    {
        var p = _member.Player;

        w.WriteD(_member.AllianceGroupId);
        w.WriteD(p.ObjectId);

        w.WriteD(p.MaxHp);
        w.WriteD(p.CurrentHp);
        w.WriteD(p.MaxMp);
        w.WriteD(p.CurrentMp);
        w.WriteD(p.MaxFp);
        w.WriteD(p.CurrentFp);

        w.WriteD(0); // unk (3.5)
        w.WriteD(p.Position.WorldId);
        w.WriteD(p.Position.WorldId); // duplicated in Java
        w.WriteF(p.Position.X);
        w.WriteF(p.Position.Y);
        w.WriteF(p.Position.Z);

        w.WriteC((byte)p.PlayerClass);
        w.WriteC((byte)p.Gender);
        w.WriteC(p.Level);

        w.WriteC(WireId(_event));
        w.WriteH(0); // channel
        w.WriteC(0);

        switch (_event)
        {
            case AllianceEvent.Leave:
            case AllianceEvent.LeaveTimeout:
            case AllianceEvent.Banned:
            case AllianceEvent.Movement:
            case AllianceEvent.Disconnected:
                break;

            case AllianceEvent.MemberGroupChange:
                w.WriteS(p.Name);
                break;

            default: // Join, Enter, EnterOffline, Update, Reconnect, AppointViceCaptain, DemoteViceCaptain, AppointCaptain
                w.WriteS(p.Name);
                w.WriteD(0);
                w.WriteD(0);
                w.WriteC(0x7F);
                w.WriteH(0); // abnormal effects count — always empty (see class doc)
                for (int i = 0; i < 8; i++) w.WriteD(0);
                break;
        }
    }
}
