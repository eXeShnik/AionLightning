using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// LFG board response. Opcode 0xA6.
/// action 0x00/0x02 = recruiter list; 0x04/0x06 = applicant list;
/// 0x01/0x03 = delete-ack (recruiter); 0x05 = delete-ack (applicant).
/// </summary>
public sealed class SM_FIND_GROUP : AionServerPacket
{
    private readonly byte _action;
    private readonly IReadOnlyList<FindGroupEntry>? _entries;
    private readonly int _objIdAck;
    private readonly int _unkAck;

    public SM_FIND_GROUP(byte action, IReadOnlyList<FindGroupEntry> entries) : base(0xA6)
    {
        _action  = action;
        _entries = entries;
    }

    // Ack for delete/update (0x01, 0x03, 0x05)
    public SM_FIND_GROUP(byte action, int objId, int unk = 0) : base(0xA6)
    {
        _action   = action;
        _objIdAck = objId;
        _unkAck   = unk;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        switch (_action)
        {
            case 0x00:
            case 0x02:
            {
                var list = _entries!;
                w.WriteH((short)list.Count);
                w.WriteH((short)list.Count);
                w.WriteD(now);
                foreach (var e in list)
                {
                    w.WriteD(e.ObjectId);
                    w.WriteD(e.IsPlayer ? 65557 : 0);
                    w.WriteC(e.GroupType);
                    w.WriteS(e.Message);
                    w.WriteS(e.Name);
                    w.WriteC(e.MemberSize);
                    w.WriteC(e.MinLevel);
                    w.WriteC(e.MaxLevel);
                    w.WriteD(e.LastUpdate);
                }
                break;
            }
            case 0x04:
            case 0x06:
            {
                var list = _entries!;
                w.WriteH((short)list.Count);
                w.WriteH((short)list.Count);
                w.WriteD(now);
                foreach (var e in list)
                {
                    w.WriteD(e.ObjectId);
                    w.WriteC(e.GroupType);
                    w.WriteS(e.Message);
                    w.WriteS(e.Name);
                    w.WriteC(e.ClassId);
                    w.WriteC(e.MinLevel);
                    w.WriteD(e.LastUpdate);
                }
                break;
            }
            case 0x01:
            case 0x03:
                w.WriteD(_objIdAck);
                w.WriteD(_unkAck);
                break;

            case 0x05:
                w.WriteD(_objIdAck);
                break;
        }
    }
}
