// Port of Java data/scripts/system/handlers/quest/katalam/_22524UnexpectedInformation.java (Romanz).
// Level-up-started (no accept dialog); kill 231212 five times (var 0->1->2->3->4 non-reward,
// then 4->5 reward) via the DefaultOnKillEventAsync span/reward-flip helpers; turn in at 800996.
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

namespace Quest.Katalam;

public sealed class _22524UnexpectedInformation : QuestHandlerBase
{
    private const int QuestIdConst = 22524;
    private const int MobId        = 231212;
    private const int NpcId        = 800996;

    public _22524UnexpectedInformation(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status != QuestStatus.REWARD || env.TargetId != NpcId) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        if (dialog == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
        if (dialog == DialogAction.SELECT_QUEST_REWARD)
            return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (await DefaultOnKillEventAsync(env, conn, MobId, 0, 1, ct)) return true;
        if (await DefaultOnKillEventAsync(env, conn, MobId, 1, 2, ct)) return true;
        if (await DefaultOnKillEventAsync(env, conn, MobId, 2, 3, ct)) return true;
        if (await DefaultOnKillEventAsync(env, conn, MobId, 3, 4, ct)) return true;
        return await DefaultOnKillEventAsync(env, conn, MobId, 4, reward: true, ct);
    }
}
