// Port of Java data/scripts/system/handlers/quest/morheim/_2040KikanantasLoyalty.java (Hellboy aion4Free).
// Zone-mission chain quest, gated behind both 2300 and 2039 (auto-started via
// OnLevelUpAsync/OnZoneMissionEndAsync). Talk 204388 (var 0->1), 204414 through var 1->2->3 (a
// CHECK_USER_HAS_QUEST_ITEM at var 2 only peeks at the quest_data.xml collect-item list without
// consuming it - Java passes collectItemCheck's remove flag as false here), back to 204388
// (var 3->4), then 204345's SET_SUCCEED flips to REWARD. Turn in at 204304, which also removes
// item 182204018 (carried over from the collect-item check) on completion.
using System.Linq;
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

namespace Quest.Morheim;

public sealed class _2040KikanantasLoyalty : QuestHandlerBase
{
    private const int QuestIdConst = 2040;
    private const int CarryOverItem = 182204018;

    private static readonly int[] _npcIds = [204388, 204414, 204304, 204345];
    private static readonly int[] _precedingQuestIds = [2300, 2039];

    private readonly IItemDao _itemDao;

    public _2040KikanantasLoyalty(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in _npcIds)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, 2039, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _precedingQuestIds, isZoneMission: true, ct);

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
            if (targetId == 204388)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return var == 0 && await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO4)
                    return var == 3 && await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }

            if (targetId == 204345)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 4 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return var == 4 && await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == 204414)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_1354)
                {
                    await PlayQuestMovieAsync(conn, player, 85, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return var == 1 && await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO3)
                    return var == 2 && await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if (var != 2) return false;
                    var collectItems = Template?.CollectItems?.Items;
                    bool hasAll = collectItems is { Count: > 0 } &&
                        collectItems.All(req => (player.Inventory.FindByItemId(req.ItemId)?.Count ?? 0) >= req.Count);
                    return await SendQuestDialogAsync(conn, targetObjId, hasAll ? 10000 : 10001, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 204304)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, CarryOverItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
