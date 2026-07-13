// Port of Java data/scripts/system/handlers/quest/eltnen/_1345BearerOfBadNews.java (Ritsu).
// Standalone quest: Demokritos (204006) starts and finishes it; Kreon (203765) advances var 0->1;
// using the Tarnished Ring (182201320) inside "LC1_ITEMUSEAREA_Q1345" consumes it and flips
// straight to REWARD (var 1->2).
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION cast delay on the ring use is applied on the same
// tick instead - the same simplification UseQuestObjectAsync's doc comment already documents.
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

namespace Quest.Eltnen;

public sealed class _1345BearerOfBadNews : QuestHandlerBase
{
    private const int QuestIdConst   = 1345;
    private const int DemokritosNpc  = 204006;
    private const int KreonNpc       = 203765;
    private const int TarnishedRingItem = 182201320;
    private const string ItemUseZone = "LC1_ITEMUSEAREA_Q1345";

    private readonly IItemDao _itemDao;

    public _1345BearerOfBadNews(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DemokritosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DemokritosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KreonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(TarnishedRingItem, QuestId);
    }

    // Java's onItemUseEvent always reports the packet handled (returns SUCCESS unconditionally,
    // even outside the START/zone gate) - preserved via the trailing "return true" fallback.
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != TarnishedRingItem) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && player.CurrentZones.Contains(ItemUseZone))
        {
            var env = new QuestEnv(null, player, QuestId, 0);
            return await UseQuestObjectAsync(env, conn, step: 1, nextStep: 2, reward: true, varNum: 0,
                addItemId: 0, addItemCount: 0, removeItemId: TarnishedRingItem, removeItemCount: 1,
                movieId: 0, dieObject: false, _itemDao, ct);
        }
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != DemokritosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != KreonNpc) return false;
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return targetId == DemokritosNpc && await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
