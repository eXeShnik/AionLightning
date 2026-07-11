// Port of Java data/scripts/system/handlers/quest/eltnen/_3732GeneralMania.java (Rinzler).
// Talk to Brunto (800518) to start; kill 256694 once (var0 0->1) then 256693 once to flip to
// REWARD; return to Brunto to finish.
// Skip vs Java: the start dialog uses the 3-arg sendQuestStartDialog(env, 182202179, 1) overload,
// which gives a flavor item on accept - no equivalent overload exists on this port's
// SendQuestStartDialogAsync helper. The item is never checked or consumed anywhere else in this
// handler, so omitting it does not affect completability.
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

public sealed class _3732GeneralMania : QuestHandlerBase
{
    private const int QuestIdConst = 3732;
    private const int BruntoNpc    = 800518;
    private const int FirstMob     = 256694;
    private const int SecondMob    = 256693;

    public _3732GeneralMania(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BruntoNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BruntoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SecondMob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != BruntoNpc) return false;
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 0)
            return await DefaultOnKillEventAsync(env, conn, FirstMob, 0, 1, ct);
        if (var == 1)
            return await DefaultOnKillEventAsync(env, conn, SecondMob, 1, reward: true, ct);
        return false;
    }
}
