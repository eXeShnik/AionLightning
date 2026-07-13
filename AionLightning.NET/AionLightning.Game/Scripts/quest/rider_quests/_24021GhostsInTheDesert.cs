// Port of Java data/scripts/system/handlers/quest/rider_quests/_24021GhostsInTheDesert.java (pralinka).
// Zone-mission sub-quest of 24020: talk to Bragi (204302, var0 0->1), talk to Tofa (204329, movie
// 73, var0 1->2), collect-check + Tofynir (802046, var0 2->3), give a Special Cube quest item at
// Tofynir (var0 3->4), use the item inside DF2_ITEMUSEAREA_Q2032 (var0 4->reward, movie 88), turn in
// at Tofa.
// Simplified vs Java: the "isFullSpecialCube" pre-check on the item hand-out (SETPRO4) is collapsed
// into GiveQuestItemAsync's own bag-capacity check - there is no separate special-cube inventory
// extension modeled in this port.
// Java bug: the outer switch(targetId) case for Tofa (204329) had no break before "case 802046:",
// so any unhandled dialog sent to Tofa fell through into Tofynir's dialog switch (running its
// CHECK_USER_HAS_QUEST_ITEM/SETPRO4 logic against the wrong NPC). Fixed here by using independent
// per-NPC if-blocks that never fall into each other.
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

namespace Quest.RiderQuests;

public sealed class _24021GhostsInTheDesert : QuestHandlerBase
{
    private const int QuestIdConst = 24021;
    private const int BragiNpc   = 204302;
    private const int TofaNpc    = 204329;
    private const int TofynirNpc = 802046;
    private const int CubeItem   = 182215363;
    private const string ItemUseZone = "DF2_ITEMUSEAREA_Q2032";

    private readonly IItemDao _itemDao;

    public _24021GhostsInTheDesert(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(CubeItem, QuestId);
        engine.RegisterQuestNpc(BragiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TofaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TofynirNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

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
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    if (var != 1) return false;
                    await PlayQuestMovieAsync(conn, player, 73, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == TofynirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                        giveItemId: CubeItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TofaNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CubeItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 4, 4, reward: true, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 88, dieObject: false, _itemDao, ct);
    }
}
