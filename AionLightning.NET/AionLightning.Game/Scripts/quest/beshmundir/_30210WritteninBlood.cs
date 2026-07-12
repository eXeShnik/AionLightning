// Port of Java data/scripts/system/handlers/quest/beshmundir/_30210WritteninBlood.java (Gigi).
// Start at 203837; turning in 30x item 182209613 there (CHECK_USER_HAS_QUEST_ITEM) flips to
// REWARD; turn in for real at the separate NPC 798941.
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

public sealed class _30210WritteninBlood : QuestHandlerBase
{
    private const int QuestIdConst = 30210;
    private const int StartNpc     = 203837;
    private const int TurnInNpc    = 798941;
    private const int BloodItemId  = 182209613;
    private const long RequiredCount = 30;

    private readonly IItemDao _itemDao;

    public _30210WritteninBlood(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != StartNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                var item = player.Inventory.FindByItemId(BloodItemId);
                if (entry.GetVar(0) == 0 && item is not null && item.Count >= RequiredCount)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, BloodItemId, RequiredCount, ct);
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
                DialogAction.USE_OBJECT        => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _ => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }
}
