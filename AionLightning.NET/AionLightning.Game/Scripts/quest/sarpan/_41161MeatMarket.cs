// Port of Java data/scripts/system/handlers/quest/sarpan/_41161MeatMarket.java (Cheatkiller).
// Talk to 205583 to start (no item); at 205567, gather the quest_data.xml collect_items and hand
// them in (var 0->1->2, reward on the second visit); turn in at 205567.
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

namespace Quest.Sarpan;

public sealed class _41161MeatMarket : QuestHandlerBase
{
    private const int QuestIdConst = 41161;
    private const int StartNpc      = 205583;
    private const int CollectorNpc  = 205567;

    private readonly IItemDao _itemDao;

    public _41161MeatMarket(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CollectorNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == CollectorNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                int var = entry.GetVar(0);
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, true, 5, 0, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == CollectorNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
