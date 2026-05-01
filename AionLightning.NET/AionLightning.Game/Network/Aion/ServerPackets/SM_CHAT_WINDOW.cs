using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Opens a whisper or group-chat window for the target player. Opcode 0x63.
/// Action 2 = group/alliance chat view; 4 = target has no group; 1 = whisper view.
/// </summary>
public sealed class SM_CHAT_WINDOW : AionServerPacket
{
    private readonly Player _target;
    private readonly bool   _isGroup;

    public SM_CHAT_WINDOW(Player target, bool isGroup) : base(0x63)
    {
        _target  = target;
        _isGroup = isGroup;
    }

    public override void Write(ref PacketWriter w)
    {
        if (_isGroup)
        {
            var group = _target.Group;
            if (group is not null)
            {
                w.WriteC(2);                                          // action = group
                w.WriteS(_target.Name);
                w.WriteD(group.GroupId);
                var leader = group.Members.FirstOrDefault(m => m.ObjectId == group.LeaderObjectId);
                w.WriteS(leader?.Name ?? string.Empty);

                var members = group.Members;
                for (int i = 0; i < 6; i++)
                    w.WriteC(i < members.Count ? (byte)members[i].Level : (byte)0);
                for (int i = 0; i < 6; i++)
                    w.WriteC(i < members.Count ? (byte)members[i].PlayerClass : (byte)0);
            }
            else
            {
                w.WriteC(4);                                          // action = no group
                w.WriteS(_target.Name);
                w.WriteD(0);
                w.WriteC((byte)_target.PlayerClass);
                w.WriteC((byte)_target.Level);
                w.WriteC(0);                                          // unk
            }
        }
        else
        {
            w.WriteC(1);                                              // action = whisper
            w.WriteS(_target.Name);
            w.WriteS(_target.Legion?.Name ?? string.Empty);
            w.WriteC((byte)_target.Level);
            w.WriteH((short)_target.PlayerClass);
            w.WriteS(string.Empty);                                   // player note — not stored
            w.WriteD(1);
        }
    }
}
