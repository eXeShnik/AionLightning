// Port of Java data/scripts/system/handlers/quest/rider_quests/_14051RootOfTheProblem.java (pralinka).
// Zone-mission sub-quest of 14050: talk to StartNpc (204500, var0 0->1), talk to CollectNpc
// (204549) which checks for the quest_data.xml collect items (var0 1->2, hands over item
// 182215339) and then re-visits for two more plain step bumps (var0 2->3), turn in at FinishNpc
// (730026, var0 3->4 + REWARD, removes the item); TurnInNpc (730024) shows the final end dialog.
// Java bug: onDialogEvent's switch on CollectNpc (204549) had no break after QUEST_SELECT, so
// talking with var0 outside {1,2} fell through into CHECK_USER_HAS_QUEST_ITEM's body - which has no
// var guard of its own in Java - and ran the full collect-item check/consume/give unconditionally
// regardless of the actual dialog id. Fixed here by gating CheckQuestItemsAsync on the actual
// CHECK_USER_HAS_QUEST_ITEM dialog only (its own step parameter still enforces var0==1).
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

namespace Quest.RiderQuests;

public sealed class _14051RootOfTheProblem : QuestHandlerBase
{
    private const int QuestIdConst  = 14051;
    private const int StartNpc      = 204500;
    private const int CollectNpc    = 204549;
    private const int FinishNpc     = 730026;
    private const int TurnInNpc     = 730024;
    private const int CollectedItem = 182215339;

    private readonly IItemDao _itemDao;

    public _14051RootOfTheProblem(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { StartNpc, CollectNpc, FinishNpc, TurnInNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14050, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
            return targetId == TurnInNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == CollectNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false,
                    checkOkId: 10000, checkFailId: 10001, giveItemId: CollectedItem, giveItemCount: 1, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO3 && var == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == FinishNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 3 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, CollectedItem, 1, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }
        return false;
    }
}
