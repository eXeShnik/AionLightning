// Port of Java data/scripts/system/handlers/quest/rider_quests/_14046PiecingTheMemory.java (pralinka).
// Zone-mission sub-quest of 14040: talk to 278500 (var0 0->1), a multi-step chain at 203834 (var0
// 1->2, movie 102; 3->4; 5->6 with item removal), collect-check at 203786 (var0 2->3, hands out
// item 182215354), using the item advances var0 4->5 (movie 170), turn in at 203754/203704.
// Java bug: onDialogEvent's switch on 203834 had no break after QUEST_SELECT, so talking with var0
// outside {1,3,5} fell through into the SELECT_ACTION_1353 body and played movie 102 unconditionally.
// Fixed here so the movie only plays on its own actual dialog id.
// Skip vs Java: onItemUseEvent's 1s cast-delay scheduling is collapsed into an immediate effect
// (same simplification as UseQuestObjectAsync/other item-use ports in this project).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.RiderQuests;

public sealed class _14046PiecingTheMemory : QuestHandlerBase
{
    private const int QuestIdConst = 14046;
    private const int Npc278500 = 278500;
    private const int Npc203834 = 203834;
    private const int Npc203786 = 203786;
    private const int Npc203754 = 203754;
    private const int Npc203704 = 203704;
    private const int MemoryItem = 182215354;

    private readonly IItemDao _itemDao;

    public _14046PiecingTheMemory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(MemoryItem, QuestId);
        foreach (int npc in new[] { Npc278500, Npc203834, Npc203786, Npc203754, Npc203704 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != Npc203704) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (env.TargetId == Npc278500)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (env.TargetId == Npc203834)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, player, 102, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            if (dialog == DialogAction.SETPRO6)
            {
                if (var != 5) return false;
                await RemoveQuestItemAsync(player, conn, _itemDao, MemoryItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            }
            return false;
        }

        if (env.TargetId == Npc203786)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: false, checkOkId: 10000, checkFailId: 10001,
                    giveItemId: MemoryItem, giveItemCount: 1, ct);
            return false;
        }

        if (env.TargetId == Npc203754)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 6 && await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != MemoryItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;

        await PlayQuestMovieAsync(conn, player, 170, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
        return true;
    }
}
