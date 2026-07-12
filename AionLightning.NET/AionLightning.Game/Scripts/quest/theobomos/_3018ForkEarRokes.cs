// Port of Java data/scripts/system/handlers/quest/theobomos/_3018ForkEarRokes.java.
// Talk to the notice board (730105) to start (no accept dialog, closes immediately); at the
// middle npc (798150), handing in the collect_items grants the Fork-Ear Rokes Tail (182208009)
// and flips to REWARD; turning in again removes a different item (182200504) — ported as-is,
// matching Java exactly (not a bug, just what the script does). The onGetItemEvent hook is a
// belt-and-suspenders re-trigger: acquiring 182208009 by any other means (loot/trade) also flips
// to REWARD if the quest var hasn't moved yet, mirrored via the Batch 0 OnItemGet hook.
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

namespace Quest.Theobomos;

public sealed class _3018ForkEarRokes : QuestHandlerBase
{
    private const int QuestIdConst = 3018;
    private const int BoardNpc     = 730105;
    private const int MiddleNpc    = 798150;
    private const int TailItemId   = 182208009;
    private const int RemoveItemId = 182200504;

    private readonly IItemDao _itemDao;

    public _3018ForkEarRokes(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BoardNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BoardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MiddleNpc).OnTalk.Add(QuestId);
        engine.RegisterItemGet(TailItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != BoardNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == MiddleNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 10, checkFailId: 5, giveItemId: TailItemId, giveItemCount: 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MiddleNpc)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, RemoveItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != TailItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }
}
