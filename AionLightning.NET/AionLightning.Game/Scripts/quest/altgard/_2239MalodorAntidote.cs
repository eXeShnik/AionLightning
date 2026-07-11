// Port of Java data/scripts/system/handlers/quest/altgard/_2239MalodorAntidote.java (Ritsu).
// Talk to Gilungk (203613), relay/collect-check at Vovetirn (203630), turn in at Gilungk.
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2239MalodorAntidote : QuestHandlerBase
{
    private const int QuestIdConst = 2239;
    private const int GilungkNpc   = 203613;
    private const int VovetirnNpc  = 203630;
    private const int AntidoteItem = 182203227;

    private readonly IItemDao _itemDao;

    public _2239MalodorAntidote(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GilungkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GilungkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VovetirnNpc).OnTalk.Add(QuestId);
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
            if (targetId == GilungkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == VovetirnNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM when var == 1:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 3, reward: false,
                            checkOkId: 10, checkFailId: 1694, giveItemId: AntidoteItem, giveItemCount: 1, ct);
                    default:
                        return false;
                }
            }
            if (targetId == GilungkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3 && var == 3)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GilungkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
