// Port of Java data/scripts/system/handlers/quest/hero/_23525ConquerTheVoidCube.java (Elyos mirror
// of _13525CompleteTheVoidCube). Auto-starts on level-up; talk to Nieber (800571) to finish
// immediately — SELECT_QUEST_REWARD flips straight to REWARD (var 0 unchanged).
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

namespace Quest.Hero;

public sealed class _23525ConquerTheVoidCube : QuestHandlerBase
{
    private const int QuestIdConst = 23525;
    private const int NieberNpc    = 800571;

    public _23525ConquerTheVoidCube(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NieberNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NieberNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != NieberNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
