// Port of Java data/scripts/system/handlers/quest/crafting/_2981ExpertAlchemistsFinalExam.java (Gigi).
// Single-npc "hand in the crafted proof item" quest: talk to the alchemy trainer (204102), turn
// in 182207950 to flip to REWARD, then collect the standard quest reward.
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

public sealed class _2981ExpertAlchemistsFinalExam : QuestHandlerBase
{
    private const int QuestIdConst = 2981;
    private const int TrainerNpc  = 204102;
    private const int ProofItemId = 182207950;

    private readonly IItemDao _itemDao;

    public _2981ExpertAlchemistsFinalExam(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

        if (status == QuestStatus.START && dialog == DialogAction.QUEST_SELECT)
        {
            if ((player.Inventory.FindByItemId(ProofItemId)?.Count ?? 0) <= 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);

            await RemoveQuestItemAsync(player, conn, _itemDao, ProofItemId, 1, ct);
            await ChangeQuestStepAsync(conn, entry!, -1, 0, toReward: true, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        }

        if (status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
