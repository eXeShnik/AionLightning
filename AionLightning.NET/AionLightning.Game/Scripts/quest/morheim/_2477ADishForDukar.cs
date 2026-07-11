// Port of Java data/scripts/system/handlers/quest/morheim/_2477ADishForDukar.java.
// Start at Dukar (204355, var 0->1 on accept); an explicit item-existence gate at 204100 checks
// for 5x182204196 (Java QuestHandler.checkItemExistence — an explicit itemId/count check distinct
// from the quest_data.xml-driven QuestHandlerBase.CheckQuestItemsAsync, so it's inlined here),
// consumes them and grants 182204234; back at 204100 finishing hands off 182204197; turn in at
// Dukar removes it.
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

namespace Quest.Morheim;

public sealed class _2477ADishForDukar : QuestHandlerBase
{
    private const int QuestIdConst  = 2477;
    private const int DukarNpc      = 204355;
    private const int CookNpc       = 204100;
    private const int IngredientId  = 182204196;
    private const int DishBatterId  = 182204234;
    private const int FinishedDishId = 182204197;

    private readonly IItemDao _itemDao;

    public _2477ADishForDukar(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DukarNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DukarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CookNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != DukarNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
                var started = player.Quests.Get(QuestId)!;
                await ChangeQuestStepAsync(conn, started, 0, 1, toReward: false, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == DukarNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckItemExistenceAsync(env, conn, 1, 2, false, IngredientId, 5, true, 10000, 10001, DishBatterId, 1, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            else if (targetId == CookNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, DishBatterId, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, FinishedDishId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: false, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DukarNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, FinishedDishId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    /// <summary>Java QuestHandler.checkItemExistence(step, nextStep, reward, itemId, itemCount,
    /// remove, checkOkId, checkFailId, giveItemId, giveItemCount) — an explicit item id/count gate,
    /// distinct from the quest_data.xml collect-items list <see cref="CheckQuestItemsAsync"/> reads.</summary>
    private async ValueTask<bool> CheckItemExistenceAsync(QuestEnv env, GsClientConnection conn,
        int step, int nextStep, bool reward, int itemId, long itemCount, bool remove,
        int checkOkId, int checkFailId, int giveItemId, long giveItemCount, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        if (entry is null || entry.GetVar(0) != step) return false;

        var item = player.Inventory.FindByItemId(itemId);
        bool has = item is not null && item.Count >= itemCount;
        if (has && remove)
            has = await RemoveQuestItemAsync(player, conn, _itemDao, itemId, itemCount, ct);

        if (!has)
            return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);

        if (giveItemId != 0 && giveItemCount != 0 && !await GiveQuestItemAsync(player, conn, _itemDao, giveItemId, giveItemCount, ct))
            return false;

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
        return await SendQuestDialogAsync(conn, targetObjId, checkOkId, ct);
    }
}
