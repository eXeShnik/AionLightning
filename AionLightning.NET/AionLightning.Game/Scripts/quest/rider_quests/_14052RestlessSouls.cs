// Port of Java data/scripts/system/handlers/quest/rider_quests/_14052RestlessSouls.java (pralinka).
// Zone-mission sub-quest of 14050: talk to TurnInNpc (204629, var0 0->1 then 1->2), talk to
// GraveyardNpc (204625) which checks the quest_data.xml collect items and hands over item
// 182215344 (var0 1->2, a second path to the same transition as TurnInNpc's own SETPRO2), then
// visit the four shrine npcs (204628/204627/204626/204622) at var0==2 - each grants one of its own
// unique items (182215340/341/342/343) without changing var0 - and GraveyardNpc's SET_SUCCEED
// flips straight to REWARD once var0==4; using AltarObj (700270) removes item 182215344 and
// advances var0 3->4. Note: the source has no visible var0 2->3 transition anywhere in this file -
// ported as-is (a pre-existing content gap in the Java quest, not a control-flow bug fixable here).
// Java bug: onDialogEvent's switches on TurnInNpc (204629) and GraveyardNpc (204625) had no break
// after their QUEST_SELECT cases, so talking with a var0 outside the guarded values fell through
// into the next case's unconditional body - TurnInNpc's fallthrough force-advanced var0 1->2 via
// SETPRO2, and GraveyardNpc's fell into CHECK_USER_HAS_QUEST_ITEM (no var guard in Java) running
// the full collect check unconditionally - regardless of the actual dialog id sent. Both are
// re-gated here on their own actual dialog id.
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Quest.RiderQuests;

public sealed class _14052RestlessSouls : QuestHandlerBase
{
    private const int QuestIdConst  = 14052;
    private const int TurnInNpc     = 204629;
    private const int GraveyardNpc  = 204625;
    private const int ShrineA       = 204628;
    private const int ShrineB       = 204627;
    private const int ShrineC       = 204626;
    private const int ShrineD       = 204622;
    private const int AltarObj      = 700270;
    private const int CollectedItem = 182215344;
    private const int ShrineAItem   = 182215340;
    private const int ShrineBItem   = 182215341;
    private const int ShrineCItem   = 182215342;
    private const int ShrineDItem   = 182215343;

    private readonly IItemDao _itemDao;

    public _14052RestlessSouls(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { TurnInNpc, GraveyardNpc, ShrineA, ShrineB, ShrineC, ShrineD, AltarObj })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14050, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
            return targetId == TurnInNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == GraveyardNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false,
                    checkOkId: 10000, checkFailId: 10001, giveItemId: CollectedItem, giveItemCount: 1, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SET_SUCCEED && var == 4)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == ShrineA) return await HandleShrineAsync(conn, player, targetObjId, dialog, var, 1694, ShrineAItem, ct);
        if (targetId == ShrineB) return await HandleShrineAsync(conn, player, targetObjId, dialog, var, 1781, ShrineBItem, ct);
        if (targetId == ShrineC) return await HandleShrineAsync(conn, player, targetObjId, dialog, var, 1864, ShrineCItem, ct);
        if (targetId == ShrineD) return await HandleShrineAsync(conn, player, targetObjId, dialog, var, 1949, ShrineDItem, ct);

        if (targetId == AltarObj)
            return dialog == DialogAction.USE_OBJECT
                && await UseQuestObjectAsync(env, conn, 3, 4, reward: false, varNum: 0,
                    addItemId: 0, addItemCount: 0, removeItemId: CollectedItem, removeItemCount: 1,
                    movieId: 0, dieObject: false, _itemDao, ct);

        return false;
    }

    /// <summary>Shared shape of the four graveyard shrines (204628/204627/204626/204622): show
    /// their intro dialog at var0==2, then on SETPRO3 grant their unique item (if not already held)
    /// and re-open the selection dialog, without advancing var0 (Java: same for all four).</summary>
    private async ValueTask<bool> HandleShrineAsync(GsClientConnection conn, Player player,
        int targetObjId, DialogAction dialog, int var, int introDialogId, int itemId, CancellationToken ct)
    {
        if (dialog == DialogAction.QUEST_SELECT)
            return var == 2 && await SendQuestDialogAsync(conn, targetObjId, introDialogId, ct);
        if (dialog == DialogAction.SETPRO3 && var == 2)
        {
            if ((player.Inventory.FindByItemId(itemId)?.Count ?? 0) == 0)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct)) return true;
            }
            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
        }
        return false;
    }
}
