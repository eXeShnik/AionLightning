// Port of Java data/scripts/system/handlers/quest/fenris_fang/_4939ProvingGround.java (Nanou/Gigi).
// Start at Kvasir (204053, plain accept); Njord (204055, var 0->1); Sichel (204273, var 1->2);
// Skadi (204054, var 2->3, then a quest_data.xml collect-item check at var 3 advances 3->4 —
// Java's QuestService.collectItemCheck, ported via QuestHandlerBase.CheckQuestItemsAsync); Balder
// (204075) requires 186000084 x1 to flip to reward; turn in at Kvasir. Talking to Kvasir again
// while the quest is active (targetId not otherwise matched) falls to Java's switch(targetId)
// default branch, which just re-shows the start dialog.
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

namespace Quest.FenrisFang;

public sealed class _4939ProvingGround : QuestHandlerBase
{
    private const int QuestIdConst = 4939;
    private const int KvasirNpc    = 204053;
    private const int NjordNpc     = 204055;
    private const int SichelNpc    = 204273;
    private const int SkadiNpc     = 204054;
    private const int BalderNpc    = 204075;
    private const int HolyWaterItem = 186000084;

    private readonly IItemDao _itemDao;

    public _4939ProvingGround(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KvasirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KvasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NjordNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SichelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SkadiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BalderNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != KvasirNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == NjordNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == SichelNpc)
            {
                if (var != 1) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == SkadiNpc)
            {
                if (var == 2)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.SETPRO3)
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                if (var == 3)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 4, reward: false, 10000, 10001, ct);
                }
                return false;
            }
            if (targetId == BalderNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 4)
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, HolyWaterItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, HolyWaterItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 2461, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            // Java switch(targetId) default: return sendQuestStartDialog(env) — reached when
            // Kvasir (not cased in this branch) is re-visited while the quest is active.
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KvasirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }
}
