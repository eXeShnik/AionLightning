// Port of Java data/scripts/system/handlers/quest/crafting/_29014MasterArmorsmithsPotential.java (Thuatan).
// Talk to the armorsmith trainer (204106) to start; the recipe-choice npc (204107) hands over
// one of two recipe items; the trainer then requires the finished proof item (182207899) to flip
// to REWARD.
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

public sealed class _29014MasterArmorsmithsPotential : QuestHandlerBase
{
    private const int QuestIdConst = 29014;
    private const int TrainerNpc = 204106;
    private const int RecipeNpc  = 204107;
    private const int Recipe1ItemId = 152206808;
    private const int Recipe2ItemId = 152206809;
    private const int ProofItemId   = 182207899;
    private const int FailDialogId  = 2716;

    private readonly IItemDao _itemDao;

    public _29014MasterArmorsmithsPotential(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var trainer = engine.RegisterQuestNpc(TrainerNpc);
        trainer.OnQuestStart.Add(QuestId);
        trainer.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RecipeNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (status == QuestStatus.NONE)
        {
            if (targetId != TrainerNpc) return false;
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 4762, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START)
        {
            if (targetId == RecipeNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO10:
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, Recipe1ItemId, 1, ct)) return true;
                        await ChangeQuestStepAsync(conn, entry!, 0, 1, toReward: false, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO20:
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, Recipe2ItemId, 1, ct)) return true;
                        await ChangeQuestStepAsync(conn, entry!, 0, 1, toReward: false, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == TrainerNpc && dialog == DialogAction.QUEST_SELECT)
            {
                if ((player.Inventory.FindByItemId(ProofItemId)?.Count ?? 0) <= 0)
                    return await SendQuestDialogAsync(conn, targetObjId, FailDialogId, ct);

                await RemoveQuestItemAsync(player, conn, _itemDao, ProofItemId, 1, ct);
                await ChangeQuestStepAsync(conn, entry!, -1, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            return false;
        }

        if (status == QuestStatus.REWARD)
        {
            if (targetId != TrainerNpc) return false;
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
