// Port of Java data/scripts/system/handlers/quest/heiron/_1636AFluteForTheFixing.java.
// Talk to Maximus (204535) to start; Utsida (203792) advances var0->1, a collect-check moves
// var1->2, then hands over the flute item 182201785 (var2->3); using the flute finishes the quest
// (flips to REWARD); turn in at Maximus.
// Skip vs Java: the item-use zone check (LF3_ITEMUSEAREA_Q1636) isn't ported (no zone-shape infra
// yet) — the flute is usable anywhere; the step-transition logic is unaffected.
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

namespace Quest.Heiron;

public sealed class _1636AFluteForTheFixing : QuestHandlerBase
{
    private const int QuestIdConst = 1636;
    private const int MaximusNpc = 204535;
    private const int UtsidaNpc  = 203792;
    private const int FluteItem  = 182201785;

    private readonly IItemDao _itemDao;

    public _1636AFluteForTheFixing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MaximusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MaximusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UtsidaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(FluteItem, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == MaximusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == UtsidaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                        giveItemId: FluteItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MaximusNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != FluteItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 3, 3, true, 0, ct);
    }
}
