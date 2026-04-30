using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Quest state change: accept (1), step/kill progress (2), delete (3), share (5). Opcode 0x7C.</summary>
public sealed class SM_QUEST_ACTION : AionServerPacket
{
    public enum ActionType : byte { Accept = 1, StepUpdate = 2, Delete = 3, Share = 5 }

    private readonly ActionType _action;
    private readonly int        _questId;
    private readonly byte       _status;
    private readonly int        _step;
    private readonly int        _sharerId;

    /// <summary>Accept or step-update constructor.</summary>
    public SM_QUEST_ACTION(int questId, ActionType action, byte status, int step) : base(0x7C)
    {
        _action  = action;
        _questId = questId;
        _status  = status;
        _step    = step;
    }

    /// <summary>Delete quest constructor (action=3).</summary>
    public SM_QUEST_ACTION(int questId) : base(0x7C)
    {
        _action  = ActionType.Delete;
        _questId = questId;
    }

    /// <summary>Quest share constructor (action=5): sends sharerId to the receiving member.</summary>
    public SM_QUEST_ACTION(int questId, int sharerId) : base(0x7C)
    {
        _action   = ActionType.Share;
        _questId  = questId;
        _sharerId = sharerId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)_action);
        w.WriteD(_questId);
        switch (_action)
        {
            case ActionType.Accept:
                w.WriteC(_status);
                w.WriteC(0);
                w.WriteD(_step);
                w.WriteH(0);
                w.WriteC(0);
                break;
            case ActionType.StepUpdate:
                w.WriteC(_status);
                w.WriteC(0);
                w.WriteD(_step);
                w.WriteH(0);
                break;
            case ActionType.Delete:
                w.WriteD(0);
                break;
            case ActionType.Share:
                w.WriteD(_sharerId);
                w.WriteD(0);
                break;
        }
    }
}
