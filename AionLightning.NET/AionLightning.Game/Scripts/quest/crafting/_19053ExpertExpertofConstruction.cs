// Port of Java data/scripts/system/handlers/quest/crafting/_19053ExpertExpertofConstruction.java (Ritsu).
// Single-npc quest: Construction trainer (798450) hands in via the quest_data.xml collect-item
// check (Java QuestService.collectItemCheck -> this port's CheckQuestItemsAsync).
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

namespace Quest.Crafting;

public sealed class _19053ExpertExpertofConstruction : QuestHandlerBase
{
    private const int QuestIdConst = 19053;
    private const int TrainerNpc = 798450;

    private readonly IItemDao _itemDao;

    public _19053ExpertExpertofConstruction(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var trainer = engine.RegisterQuestNpc(TrainerNpc);
        trainer.OnQuestStart.Add(QuestId);
        trainer.OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != TrainerNpc) return false;

        if (status == QuestStatus.NONE)
        {
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 1011, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 5, checkFailId: 2716, ct);
            }
            return false;
        }

        if (status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
