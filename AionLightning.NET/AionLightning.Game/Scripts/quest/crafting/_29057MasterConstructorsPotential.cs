// Port of Java data/scripts/system/handlers/quest/crafting/_29057MasterConstructorsPotential.java (Ritsu).
// Asmodian mirror of _19057MasterConstructorsPotential: trainer (798452), recipe-choice npc
// (798453), blueprint items 152208541/152208542, recipe ids 155008541/155008542. Same kinah
// costs (167500 / 223000) and dialog ids as the Elyos version.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Crafting;

public sealed class _29057MasterConstructorsPotential : QuestHandlerBase
{
    private const int QuestIdConst = 29057;
    private const int TrainerNpc = 798452;
    private const int RecipeNpc  = 798453;
    private const int KinahItemId = 182400001;

    private static readonly int[] RecipeItemIds = [152208541, 152208542];
    private static readonly int[] RecipeIds      = [155008541, 155008542];
    private static readonly long[] KinahCosts     = [167500, 223000];

    private readonly IItemDao _itemDao;

    public _29057MasterConstructorsPotential(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
            int var = entry!.GetVar(0);

            if (targetId == RecipeNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                    case DialogAction.SETPRO10:
                        return await BuyBlueprintAsync(player, conn, targetObjId, 0, ct);
                    case DialogAction.SETPRO20:
                        return await BuyBlueprintAsync(player, conn, targetObjId, 1, ct);
                }
                return false;
            }

            if (targetId == TrainerNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckTurnInAsync(env, conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (status == QuestStatus.REWARD && targetId == TrainerNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    private async ValueTask<bool> BuyBlueprintAsync(Player player, GsClientConnection conn, int targetObjId, int index, CancellationToken ct)
    {
        long kinah = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
        if (kinah < KinahCosts[index])
            return await SendQuestDialogAsync(conn, targetObjId, 4400, ct);

        if (!await GiveQuestItemAsync(player, conn, _itemDao, RecipeItemIds[index], 1, ct)) return true;
        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, KinahCosts[index], ct);

        var entry = player.Quests.Get(QuestId)!;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
    }

    private async ValueTask<bool> CheckTurnInAsync(QuestEnv env, GsClientConnection conn, int targetObjId, CancellationToken ct)
    {
        var player = env.Player;
        var collectItems = Template?.CollectItems?.Items;
        bool hasAll = collectItems is { Count: > 0 }
            && collectItems.All(req => (player.Inventory.FindByItemId(req.ItemId)?.Count ?? 0) >= req.Count);

        if (hasAll)
            return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 0, reward: true, checkOkId: 5, checkFailId: 3398, ct);

        int checkFailId = 3398;
        if (player.KnownRecipes.Contains(RecipeIds[0]) || player.KnownRecipes.Contains(RecipeIds[1]))
            checkFailId = 2716;
        else if ((player.Inventory.FindByItemId(RecipeItemIds[0])?.Count ?? 0) > 0
                 || (player.Inventory.FindByItemId(RecipeItemIds[1])?.Count ?? 0) > 0)
            checkFailId = 3057;

        if (checkFailId == 3398)
        {
            var entry = player.Quests.Get(QuestId)!;
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        }

        return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);
    }
}
