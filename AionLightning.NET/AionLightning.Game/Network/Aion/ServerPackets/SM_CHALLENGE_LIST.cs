using AionLightning.Commons.Network;
using AionLightning.Game.Model.Challenge;
using AionLightning.Game.Model.Templates.Challenge;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Port of Java network.aion.serverpackets.SM_CHALLENGE_LIST — the legion/town challenge task list
/// (action 2) and per-task quest breakdown (action 7), sent in response to CM_CHALLENGE_LIST.
/// TODO: verify opcode — 0xFB is an unused placeholder pending a live 4.6 client capture; sending is
/// gated off by default via <see cref="Configs.Options.ChallengeOptions.Enable"/> until then.
/// </summary>
public sealed class SM_CHALLENGE_LIST : AionServerPacket
{
    private readonly byte _action;
    private readonly int _ownerId;
    private readonly ChallengeType _ownerType;
    private readonly int _playerObjectId;
    private readonly IReadOnlyList<ChallengeTask>? _tasks;
    private readonly ChallengeTask? _task;

    /// <summary>Action 2 — the owner's full available-task list.</summary>
    public static SM_CHALLENGE_LIST List(int ownerId, ChallengeType ownerType, int playerObjectId, IReadOnlyList<ChallengeTask> tasks) =>
        new(2, ownerId, ownerType, playerObjectId, tasks, null);

    /// <summary>Action 7 — one task's per-quest progress breakdown.</summary>
    public static SM_CHALLENGE_LIST TaskDetail(int ownerId, ChallengeType ownerType, int playerObjectId, ChallengeTask task) =>
        new(7, ownerId, ownerType, playerObjectId, null, task);

    private SM_CHALLENGE_LIST(byte action, int ownerId, ChallengeType ownerType, int playerObjectId,
        IReadOnlyList<ChallengeTask>? tasks, ChallengeTask? task) : base(0xFB)
    {
        _action         = action;
        _ownerId        = ownerId;
        _ownerType      = ownerType;
        _playerObjectId = playerObjectId;
        _tasks          = tasks;
        _task           = task;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        w.WriteD(_ownerId);           // legionId or townId
        w.WriteC((byte)_ownerType);   // 1 for legion, 2 for town
        w.WriteD(_playerObjectId);

        switch (_action)
        {
            case 2: // challenge task list
                w.WriteD((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                w.WriteH(_tasks?.Count ?? 0);
                if (_tasks is not null)
                {
                    foreach (var task in _tasks)
                    {
                        w.WriteD(32); // unk
                        w.WriteD(task.TaskId);
                        w.WriteC(1);  // unk
                        w.WriteC(21); // unk
                        w.WriteC(0);  // unk
                        w.WriteD((int)new DateTimeOffset(task.CompleteTime, TimeSpan.Zero).ToUnixTimeSeconds());
                    }
                }
                break;

            case 7: // individual challenge task info
                if (_task is not null)
                {
                    w.WriteD(32); // unk
                    w.WriteD(_task.TaskId);
                    w.WriteH(_task.QuestsCount);
                    foreach (var quest in _task.Quests.Values)
                    {
                        w.WriteD(quest.QuestId);
                        w.WriteH(quest.MaxRepeats);
                        w.WriteD(quest.ScorePerQuest);
                        w.WriteH(quest.CompleteCount);
                    }
                }
                break;
        }
    }
}
