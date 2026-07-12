// Port of Java data/scripts/system/handlers/quest/raksang/_18710TippingtheScales.java (zhkchi).
// Talk to npc 799436 to start and to turn in; a multi-tier "trade tickets" quest: handing over 4,
// 7, or 10 of item 182006427 picks reward tier var 1/2/3 respectively and flips straight to REWARD;
// re-visiting the npc replays the matching confirm page, and any further interaction finishes the
// quest at the reward index matching the tier chosen (Java's packed-vars-minus-one rewardIndex).
// Skip vs Java: the `qs.canRepeat()` re-offer check isn't ported, same pre-existing gap noted on
// _18709ReachingOuttothePast.
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

namespace Quest.Raksang;

public sealed class _18710TippingtheScales : QuestHandlerBase
{
    private const int QuestIdConst = 18710;
    private const int StartNpc     = 799436;
    private const int TicketItem   = 182006427;

    private readonly IItemDao _itemDao;

    public _18710TippingtheScales(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            (int need, int page, int tier) = dialog switch
            {
                DialogAction.SELECT_ACTION_1011  => (4, 5, 1),
                DialogAction.SELECT_ACTION_1352  => (7, 6, 2),
                DialogAction.SELECT_ACTION_1693  => (10, 7, 3),
                _ => (0, 0, 0),
            };
            if (need == 0) return false;

            var ticket = player.Inventory.FindByItemId(TicketItem);
            if ((ticket?.Count ?? 0) >= need)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, TicketItem, need, ct);
                entry.CompleteCount = 0;
                await ChangeQuestStepAsync(conn, entry, 0, tier, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, page, ct);
            }
            return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 8, ct);
                // Java switch fallthrough: USE_OBJECT with var outside 1-4 falls into the
                // SELECTED_QUEST_NOREWARD finish-quest body below.
            }
            if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await FinishQuestAsync(conn, player, entry.Step - 1, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        return false;
    }
}
