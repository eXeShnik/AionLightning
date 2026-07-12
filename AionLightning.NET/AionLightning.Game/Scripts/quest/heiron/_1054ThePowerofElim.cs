// Port of Java data/scripts/system/handlers/quest/heiron/_1054ThePowerofElim.java.
// Talk to Trajanus (730024) to start; collect items from Daminu (730008, gives 182201606) and
// Lodas (730019, gives 182201607), hand both to the Voice of Arbolu (204647) for the reward.
// Mission-chain quest (no NPC quest-offer dialog), gated on 1500.
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

namespace Quest.Heiron;

public sealed class _1054ThePowerofElim : QuestHandlerBase
{
    private const int QuestIdConst = 1054;
    private const int TrajanusNpc = 730024;
    private const int ArboluNpc   = 204647;
    private const int DaminuNpc   = 730008;
    private const int LodasNpc    = 730019;
    private const int ItemOne     = 182201606;
    private const int ItemTwo     = 182201607;

    private readonly IItemDao _itemDao;

    public _1054ThePowerofElim(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(TrajanusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArboluNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DaminuNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LodasNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ArboluNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (targetId == TrajanusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (targetId == ArboluNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SELECT_ACTION_2376)
            {
                bool hasBoth = player.Inventory.FindByItemId(ItemOne) is { Count: > 0 }
                            && player.Inventory.FindByItemId(ItemTwo) is { Count: > 0 };
                return await SendQuestDialogAsync(conn, targetObjId, hasBoth ? 2376 : 2461, ct);
            }
            if (dialog == DialogAction.SELECT_ACTION_2377)
                return await PlayQuestMovieAndReturnFalseAsync(conn, player, 187, ct);
            if (dialog == DialogAction.SETPRO5)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemOne, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemTwo, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 5, true, 5, 10001, ct);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
        }
        else if (targetId == DaminuNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                    giveItemId: ItemOne, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
        }
        else if (targetId == LodasNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    giveItemId: ItemTwo, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
        }
        return false;
    }

    private async ValueTask<bool> PlayQuestMovieAndReturnFalseAsync(GsClientConnection conn, Player player, int movieId, CancellationToken ct)
    {
        await PlayQuestMovieAsync(conn, player, movieId, ct);
        return false;
    }
}
