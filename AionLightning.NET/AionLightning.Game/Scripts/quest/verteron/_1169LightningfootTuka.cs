// Port of Java data/scripts/system/handlers/quest/verteron/_1169LightningfootTuka.java (vlog).
// Talk to Kubu (203126) to start; kill 210317 once to flip straight to REWARD; turn in at Kubu.
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1169LightningfootTuka : QuestHandlerBase
{
    private const int QuestIdConst = 1169;
    private const int KubuNpc      = 203126;
    private const int MobNpc       = 210317;

    public _1169LightningfootTuka(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KubuNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KubuNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobNpc, startVar: 0, reward: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        if (env.TargetId != KubuNpc) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
