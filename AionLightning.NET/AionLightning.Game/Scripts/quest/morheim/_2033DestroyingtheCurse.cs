// Port of Java data/scripts/system/handlers/quest/morheim/_2033DestroyingtheCurse.java (Erin, reworked vlog).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk Urakon (204391, var 0->1), Kellan (790020) through var 1->2 ->3 (requires a
// collect item) ->4 (hands out the cursed necklace, item 182204007), Kimssi (204393, var 4->5,
// movie 74), then any of the five Skurv guide npcs (204394-204398, var 5->6); using the necklace
// while inside DF2_ITEMUSEAREA_Q2033 finishes the quest (movie 75, item consumed). Turn in at
// Kellan.
// Java quirks ported as-is (no observable behavior change): the QUEST_SELECT switch at Kellan and
// the SETPRO4/FINISH_DIALOG cases fall through in the original when their guard var doesn't match -
// each fallthrough target has its own step guard that fails identically, so this port implements
// every dialog as an independent guarded branch with the same net behavior.
// Skip vs Java: Inventory.isFullSpecialCube() (a separate title-storage bag) isn't modelled here;
// GiveQuestItemAsync's own main-inventory capacity guard substitutes. onItemUseEvent's fallback
// `return HandlerResult.SUCCESS` when the var/zone guard fails is not replicated (this port returns
// false/"unhandled" there instead, which only affects internal dispatch bookkeeping, not gameplay).
using System;
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

namespace Quest.Morheim;

public sealed class _2033DestroyingtheCurse : QuestHandlerBase
{
    private const int QuestIdConst = 2033;
    private const int UrakonNpc    = 204391;
    private const int KellanNpc    = 790020;
    private const int KimssiNpc    = 204393;
    private const int NecklaceItem = 182204007;
    private const string ItemUseZone = "DF2_ITEMUSEAREA_Q2033";

    private static readonly int[] _skurvNpcs = [204394, 204395, 204396, 204397, 204398];

    private readonly IItemDao _itemDao;

    public _2033DestroyingtheCurse(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(NecklaceItem, QuestId);
        engine.RegisterQuestNpc(UrakonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KellanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KimssiNpc).OnTalk.Add(QuestId);
        foreach (int npc in _skurvNpcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 6) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, step: 6, nextStep: 6, reward: true, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: NecklaceItem, removeItemCount: 1, movieId: 75,
            dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == UrakonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == KellanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    if (var != 3) return false;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, NecklaceItem, 1, ct)) return true;
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return var == 2 && await DefaultCloseDialogAsync(env, conn, 2, 2, ct);
                return false;
            }

            if (targetId == KimssiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_ACTION_2376 && var == 4)
                {
                    await PlayQuestMovieAsync(conn, player, 74, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2376, ct);
                }
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }

            if (Array.IndexOf(_skurvNpcs, targetId) >= 0)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KellanNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
