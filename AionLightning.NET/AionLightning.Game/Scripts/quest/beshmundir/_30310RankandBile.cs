// Port of Java data/scripts/system/handlers/quest/beshmundir/_30310RankandBile.java (Gigi).
// Talk to 204225 to start; back at 204225 (var 0), CHECK_USER_HAS_QUEST_ITEM validates 40x item
// 182209713, consumes them, and flips straight to REWARD; turn in at 799322 (USE_OBJECT shows a
// preview page, SELECT_QUEST_REWARD shows the reward list, any other dialog falls through to the
// normal end-dialog flow).
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

namespace Quest.Beshmundir;

public sealed class _30310RankandBile : QuestHandlerBase
{
    private const int QuestIdConst   = 30310;
    private const int StartNpc       = 204225;
    private const int TurnInNpc      = 799322;
    private const int BileItem       = 182209713;
    private const long RequiredCount = 40;

    private readonly IItemDao _itemDao;

    public _30310RankandBile(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                var item = player.Inventory.FindByItemId(BileItem);
                if (entry.GetVar(0) == 0 && item is not null && item.Count >= RequiredCount)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, BileItem, RequiredCount, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            return dialog switch
            {
                DialogAction.USE_OBJECT          => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _ => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }
}
