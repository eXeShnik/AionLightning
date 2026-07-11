// Port of Java data/scripts/system/handlers/quest/morheim/_2345OrashunerkSpecialOrder.java.
// Start at 798084; a collect-item check (var 0->1) gates progress; SETPRO10/20 pick a reward tier
// (var 10 or 20), granting a different item and flipping to REWARD; turning in at 204339 removes
// that item and finishes with the matching tier (Java's sendQuestEndDialog(env, reward) two-step
// dialog). Reward index is derived from the persisted var (10 -> 0, 20 -> 1) rather than Java's
// shared instance field. The 700238 "looting" branch (gather-point flavor, gated on carrying < 3
// of an item the drop table replenishes elsewhere) is a no-op here too — nothing in this port
// currently increases that item count from talking to it, matching Java's own script scope.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Morheim;

public sealed class _2345OrashunerkSpecialOrder : QuestHandlerBase
{
    private const int QuestIdConst  = 2345;
    private const int StartNpc      = 798084;
    private const int GatherObj     = 700238;
    private const int TurnInNpc     = 204339;
    private const int GatherItemId  = 182204136;
    private const int FirstTierItem = 182204137;
    private const int SecondTierItem = 182204138;

    private readonly IItemDao _itemDao;

    public _2345OrashunerkSpecialOrder(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GatherObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == StartNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        return false;
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, false, 10000, 10001, ct);
                    case DialogAction.SELECT_ACTION_1353:
                        return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                    case DialogAction.SELECT_ACTION_1438:
                        return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                    case DialogAction.SETPRO10:
                        await GiveQuestItemAsync(player, conn, _itemDao, FirstTierItem, 1, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: true, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO20:
                        await GiveQuestItemAsync(player, conn, _itemDao, SecondTierItem, 1, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 20, toReward: true, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            if (targetId == GatherObj && (player.Inventory.FindByItemId(GatherItemId)?.Count ?? 0) < 3)
                return true;
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var == 10)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, FirstTierItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                if (var == 20)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SecondTierItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
            }
            return await SendQuestEndDialogWithRewardAsync(env, conn, var == 20 ? 1 : 0, ct);
        }
        return false;
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes
    /// with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
