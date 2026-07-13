// Port of Java data/scripts/system/handlers/quest/argent_manor/_30410DreamstoReality.java (Ritsu).
// Talk to the start npc (799539) to accept; the collect-item check (CHECK_USER_HAS_QUEST_ITEM_SIMPLE
// -> QuestService.collectItemCheck) either consumes the quest_data.xml collect items and flips to
// REWARD (dialog 5) or closes the dialog on failure; turn in at 799539.
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

namespace Quest.ArgentManor;

public sealed class _30410DreamstoReality : QuestHandlerBase
{
    private const int QuestIdConst = 30410;
    private const int StartNpc     = 799539;

    private readonly IItemDao _itemDao;

    public _30410DreamstoReality(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
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
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId != StartNpc) return false;
            return dialog switch
            {
                DialogAction.QUEST_SELECT when var == 0             => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE        => await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 5, checkFailId: 0, ct),
                _                                                    => false,
            };
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
