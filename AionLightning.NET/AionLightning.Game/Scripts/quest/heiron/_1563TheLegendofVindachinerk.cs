// Port of Java data/scripts/system/handlers/quest/heiron/_1563TheLegendofVindachinerk.java.
// Talk to Poporinerk (798096) to start; using Jaiorunerk's Diary (182201729) sets var1; turn in
// either at Poporinerk (collect-check -> reward tier 0) or Kohrunerk (279005, collect-check ->
// var2, reward tier 1) — whichever the player completes the hand-in with.
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

public sealed class _1563TheLegendofVindachinerk : QuestHandlerBase
{
    private const int QuestIdConst  = 1563;
    private const int PoporinerkNpc = 798096;
    private const int KohrunerkNpc  = 279005;
    private const int DiaryItem     = 182201729;

    private readonly IItemDao _itemDao;

    public _1563TheLegendofVindachinerk(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(PoporinerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(PoporinerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KohrunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(DiaryItem, QuestId);
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
            if (targetId == PoporinerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == PoporinerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 0, true, 5, 1353, ct);
            }
            else if (targetId == KohrunerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, true, 6, 1439, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD && dialog == DialogAction.SELECT_QUEST_REWARD)
        {
            if (targetId == PoporinerkNpc) return await FinishQuestAsync(conn, player, 0, ct);
            if (targetId == KohrunerkNpc) return await FinishQuestAsync(conn, player, 1, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DiaryItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        entry.SetVar(0, 1);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
