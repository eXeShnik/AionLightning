// Port of Java data/scripts/system/handlers/quest/morheim/_2032GuardianSpirit.java (Erin, reworked vlog).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk Bragi (204302, var 0->1), then Tofa (204329) through var 1->2 (movie 73) ->3
// (requires holding a collect item, quest_data.xml driven) ->4 (hands out item 182204005); using
// that item while inside DF2_ITEMUSEAREA_Q2032 finishes the quest (movie 88, item consumed).
// Java quirk ported as-is (no observable behavior change): the QUEST_SELECT/SELECT_ACTION_1353
// switch cases at Tofa fall through to the SETPRO2 case in the original when var doesn't match -
// but defaultCloseDialog's own step guard (var must be 1) makes that fallthrough a no-op for every
// other var value, so this port implements each dialog as an independent guarded branch instead
// with identical net behavior.
// Skip vs Java: Inventory.isFullSpecialCube() (a separate title-storage bag, not modelled in this
// port) is not checked before granting the quest item; GiveQuestItemAsync's own main-inventory
// capacity guard is used instead (same simplification as other title/cube-checking ports).
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

public sealed class _2032GuardianSpirit : QuestHandlerBase
{
    private const int QuestIdConst = 2032;
    private const int BragiNpc     = 204302;
    private const int TofaNpc      = 204329;
    private const int ItemId       = 182204005;
    private const string ItemUseZone = "DF2_ITEMUSEAREA_Q2032";

    private readonly IItemDao _itemDao;

    public _2032GuardianSpirit(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(BragiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TofaNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, step: 4, nextStep: 4, reward: true, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: ItemId, removeItemCount: 1, movieId: 88,
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
            if (targetId == BragiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == TofaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    if (var != 1) return false;
                    await PlayQuestMovieAsync(conn, player, 73, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                        ItemId, 1, 0, 0, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TofaNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
