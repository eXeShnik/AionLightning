// Port of Java data/scripts/system/handlers/quest/morheim/_2031PetrifyingElim.java (Mcrizza).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE, same as every other 203x morheim chain quest - there is no OnTalk/OnQuestStart entry
// point in the Java source either). Talk Vili (204304, var 0->1), then Nabaru (730038) through
// var 1->2 (movie 71) ->3 (requires holding item 182204001) ->4; using item 182204001 while inside
// DF2_ITEMUSEAREA_Q2031 finishes the quest (movie 72, item consumed, straight to REWARD).
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION pair around the item-use finish is collapsed into an
// immediate effect (same simplification as _2321SpyTheSpiritsLetter/_2435TheBlueVineNecklace).
// Java quirk ported as-is: the SELECT_ACTION_1353 branch (movie 71) falls through to the method's
// final `return false` in the original (no dialog ack sent) - preserved here.
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

public sealed class _2031PetrifyingElim : QuestHandlerBase
{
    private const int QuestIdConst = 2031;
    private const int ViliNpc      = 204304;
    private const int NabaruNpc    = 730038;
    private const int ItemId       = 182204001;
    private const string ItemUseZone = "DF2_ITEMUSEAREA_Q2031";

    private readonly IItemDao _itemDao;

    public _2031PetrifyingElim(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(ViliNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NabaruNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.COMPLETE) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        await PlayQuestMovieAsync(conn, player, 72, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
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
            if (targetId == ViliNpc)
            {
                if (var != 0) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == NabaruNpc)
            {
                if (var == 1)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.SELECT_ACTION_1353)
                    {
                        await PlayQuestMovieAsync(conn, player, 71, ct);
                        return false; // Java: falls through with no dialog ack - see header note.
                    }
                    if (dialog == DialogAction.SETPRO2)
                    {
                        entry.SetVar(0, 2);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    }
                    return false;
                }

                if (var == 2)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    {
                        if ((player.Inventory.FindByItemId(ItemId)?.Count ?? 0) > 0)
                        {
                            entry.SetVar(0, 3);
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                    }
                    return false;
                }

                if (var == 3)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.SETPRO4)
                    {
                        entry.SetVar(0, 4);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    }
                    return false;
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NabaruNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
