// Port of Java data/scripts/system/handlers/quest/eltnen/_1351EarningMaranasRespect.java (Atomics).
// Talk to Castor (203965) to start; hand 10 of item 182201321 to Marana (203983) to finish.
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

public sealed class _1351EarningMaranasRespect : QuestHandlerBase
{
    private const int QuestIdConst  = 1351;
    private const int CastorNpc     = 203965;
    private const int MaranaNpc     = 203983;
    private const int TributeItemId = 182201321;

    private readonly IItemDao _itemDao;

    public _1351EarningMaranasRespect(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(CastorNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CastorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MaranaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == CastorNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == MaranaNpc)
        {
            if (entry is { Status: QuestStatus.START })
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    long itemCount = player.Inventory.FindByItemId(TributeItemId)?.Count ?? 0;
                    if (itemCount > 9)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, TributeItemId, 10, ct);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry is { Status: QuestStatus.REWARD })
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
