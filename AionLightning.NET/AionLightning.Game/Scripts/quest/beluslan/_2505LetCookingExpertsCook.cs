// Port of Java data/scripts/system/handlers/quest/beluslan/_2505LetCookingExpertsCook.java
// (VladimirZ, modified apozema). Talk to the AI (204720) to start, giving a meal ticket item;
// hand the ticket to Aurvandil (204731), who exchanges it for the cooked dish (var 0->1); return
// to the AI to flip to REWARD and finish. Simplification vs Java: the relay step (removeItem +
// giveItem + var advance) is persisted with a single SM_QUEST_ACTION instead of Java's two
// back-to-back identical packets (its first update fires before the var write even lands).
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

namespace Quest.Beluslan;

public sealed class _2505LetCookingExpertsCook : QuestHandlerBase
{
    private const int QuestIdConst  = 2505;
    private const int AiNpc         = 204720;
    private const int AurvandilNpc  = 204731;
    private const int TicketItem    = 182204404;
    private const int CookedItem    = 182204405;

    private readonly IItemDao _itemDao;

    public _2505LetCookingExpertsCook(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AiNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AurvandilNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == AiNpc && entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, TicketItem, 1, ct))
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (var == 1)
        {
            if (targetId == AiNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        if (targetId == AurvandilNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO1 when var == 0:
                    await RemoveQuestItemAsync(player, conn, _itemDao, TicketItem, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, CookedItem, 1, ct);
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        return false;
    }
}
