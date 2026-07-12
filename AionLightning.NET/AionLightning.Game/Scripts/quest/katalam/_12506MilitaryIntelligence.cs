// Port of Java data/scripts/system/handlers/quest/katalam/_12506MilitaryIntelligence.java (Ritsu).
// Talk to 801026 to start; 801760 advances var 0->1; back at 801026 CHECK_USER_HAS_QUEST_ITEM_SIMPLE
// checks quest_data.xml's collect_item list and flips to REWARD (page 5) with no fail page.
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

namespace Quest.Katalam;

public sealed class _12506MilitaryIntelligence : QuestHandlerBase
{
    private const int QuestIdConst = 12506;
    private const int MainNpc      = 801026;
    private const int SecondNpc    = 801760;

    private readonly IItemDao _itemDao;

    public _12506MilitaryIntelligence(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MainNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MainNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != MainNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == MainNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, true, 5, 0, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == MainNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
