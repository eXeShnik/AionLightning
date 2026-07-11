// Port of Java data/scripts/system/handlers/quest/eltnen/_3326TheShugoMenace.java (Balthazar).
// Talk to 798053 to start; kill any of 5 mob ids 20 times (var 0); turn in at the same NPC.
// Note: Java allows restarting this quest from COMPLETE status (a repeatable daily); this port's
// shared SendQuestStartDialogAsync helper only creates a fresh entry when the player has none at
// all (matches the broader repeatable-quest limitation already documented in QuestEngine.cs -
// ComputeNearbyQuests's own remark about the missing repeatable-count field), so only the first
// completion is portable here; re-accepting after COMPLETE is not yet supported anywhere in this
// engine port.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Eltnen;

public sealed class _3326TheShugoMenace : QuestHandlerBase
{
    private const int QuestIdConst = 3326;
    private const int NpcId        = 798053;
    private const int RequiredKills = 20;

    private static readonly int[] _mobs = [210897, 210939, 210873, 210919, 211754];

    public _3326TheShugoMenace(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != NpcId) return false;
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status is QuestStatus.NONE or QuestStatus.COMPLETE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                case DialogAction.SELECT_QUEST_REWARD:
                    if (entry.GetVar(0) != RequiredKills) return false;
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        bool isTracked = targetId == _mobs[0] || targetId == _mobs[1] || targetId == _mobs[2]
            || targetId == _mobs[3] || targetId == _mobs[4];
        if (!isTracked) return false;

        int var = entry.GetVar(0);
        if (var < 0 || var >= RequiredKills) return false;

        await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
        return true;
    }
}
