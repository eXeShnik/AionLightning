// Port of Java data/scripts/system/handlers/quest/inggison/_11149TheLadyLayout.java.
// Talk to 296491 to start; then move within onAtDistance range of all three statues
// 206155/206156/206157 (each sets its own var 1/2/3). Once all three are visited the quest flips to
// REWARD. Turn in at 296491.
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

namespace Quest.Inggison;

public sealed class _11149TheLadyLayout : QuestHandlerBase
{
    private const int QuestIdConst = 11149;
    private const int StartNpc = 296491;
    private const int Statue1  = 206155;
    private const int Statue2  = 206156;
    private const int Statue3  = 206157;

    public _11149TheLadyLayout(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, Statue1);
        RegisterOnAtDistance(engine, Statue2);
        RegisterOnAtDistance(engine, Statue3);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (env.TargetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == Statue1 && entry.GetVar(1) == 0)
        {
            entry.SetVar(1, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            await CheckRewardAsync(conn, entry, ct);
            return true;
        }
        if (env.TargetId == Statue2 && entry.GetVar(2) == 0)
        {
            entry.SetVar(2, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            await CheckRewardAsync(conn, entry, ct);
            return true;
        }
        if (env.TargetId == Statue3 && entry.GetVar(3) == 0)
        {
            entry.SetVar(3, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            await CheckRewardAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    private async ValueTask CheckRewardAsync(GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        if (entry.GetVar(1) == 1 && entry.GetVar(2) == 1 && entry.GetVar(3) == 1)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
    }
}
