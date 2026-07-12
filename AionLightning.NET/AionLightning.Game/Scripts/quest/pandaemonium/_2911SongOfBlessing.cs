// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2911SongOfBlessing.java.
// Accept at 204079; at 204193 (already active, not COMPLETE) SETPRO1/SETPRO2 pick one of two
// reward tiers (var 2 -> page 5, var 3 -> page 6), both flipping straight to REWARD; any other
// dialog at 204193 while active turns the quest in.
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

namespace Quest.Pandaemonium;

public sealed class _2911SongOfBlessing : QuestHandlerBase
{
    private const int QuestIdConst = 2911;
    private const int StartNpc = 204079;
    private const int StepNpc = 204193;

    public _2911SongOfBlessing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == StepNpc && entry is not null && entry.Status is not QuestStatus.COMPLETE)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            if (dialog == DialogAction.SETPRO2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
