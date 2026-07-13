// Port of Java data/scripts/system/handlers/quest/heiron/_1661FindingTheForges.java.
// Talk to 204600 to start (plays movie 200 on accept); then move within onAtDistance range of the
// three forges 206045 -> 206046 -> 206047 in order (var0 0 -> 16 -> 48 -> REWARD). Turn in at 204600.
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

namespace Quest.Heiron;

public sealed class _1661FindingTheForges : QuestHandlerBase
{
    private const int QuestIdConst = 1661;
    private const int StartNpc = 204600;
    private const int Forge1   = 206045;
    private const int Forge2   = 206046;
    private const int Forge3   = 206047;

    public _1661FindingTheForges(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, Forge1);
        RegisterOnAtDistance(engine, Forge2);
        RegisterOnAtDistance(engine, Forge3);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (env.TargetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await PlayQuestMovieAsync(conn, player, 200, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (env.TargetId == Forge1 && var == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 16, toReward: false, ct);
            return true;
        }
        if (env.TargetId == Forge2 && var == 16)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 48, toReward: false, ct);
            return true;
        }
        if (env.TargetId == Forge3 && var == 48)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 48, toReward: true, ct);
            return true;
        }
        return false;
    }
}
