// Port of Java data/scripts/system/handlers/quest/katalam/_20082KahrunWonT.java (Cheatkiller).
// Asmodian mirror of 10082: 801239 -> 800536 -> 800533 (collect) -> onAtDistance near 800534
// (movie 824, var 3->4) -> 800534 (SET_SUCCEED -> reward) -> turn in 800537.
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

public sealed class _20082KahrunWonT : QuestHandlerBase
{
    private const int QuestIdConst = 20082;

    private readonly IItemDao _itemDao;

    public _20082KahrunWonT(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnAtDistance(engine, 800534);
        foreach (int npcId in new[] { 801239, 800536, 800533, 800534, 800537 })
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20081, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == 801239)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=0) falls into SETPRO1
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == 800536)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=1) falls into SETPRO2
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == 800533)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=2) falls into CHECK_USER_HAS_QUEST_ITEM
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
            if (targetId == 800534)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=4) falls into SET_SUCCEED
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD && targetId == 800537)
        {
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
        if (entry.GetVar(0) == 3)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            await PlayQuestMovieAsync(conn, env.Player, 824, ct);
            return true;
        }
        return false;
    }
}
