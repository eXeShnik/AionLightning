// Port of Java data/scripts/system/handlers/quest/eltnen/_1464AGiftofLove.java (Balthazar).
// Talk to 204424 to start; hand over 15 of item 152000455 at the same NPC to flip to REWARD;
// finish at 203755.
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

namespace Quest.Eltnen;

public sealed class _1464AGiftofLove : QuestHandlerBase
{
    private const int QuestIdConst  = 1464;
    private const int GiverNpc      = 204424;
    private const int TurnInNpc     = 203755;
    private const int GiftItemId    = 152000455;
    private const int RequiredCount = 15;

    private readonly IItemDao _itemDao;

    public _1464AGiftofLove(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GiverNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GiverNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GiverNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == GiverNpc && dialog == DialogAction.QUEST_SELECT)
        {
            long itemCount = player.Inventory.FindByItemId(GiftItemId)?.Count ?? 0;
            if (entry.GetVar(0) == 0 && itemCount >= RequiredCount)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
            }
            return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
