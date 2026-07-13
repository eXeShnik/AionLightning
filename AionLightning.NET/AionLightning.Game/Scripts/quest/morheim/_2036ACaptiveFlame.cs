// Port of Java data/scripts/system/handlers/quest/morheim/_2036ACaptiveFlame.java (Hellboy aion4Free).
// Zone-mission chain quest, gated behind 2035 being COMPLETE (auto-started via
// OnLevelUpAsync/OnZoneMissionEndAsync). Talk 204407 (var 0->1), 204408 (var 1->2, movie 79 on
// SELECT_ACTION_1353), killing 212878 while at var 2 jumps straight to var 4 (a +2 bump, not the
// usual +1), using object 700236 hands out item 182204014 if not already held, back to 204407
// (var 4) checks the quest_data.xml collect-item list to flip to REWARD. Turn in at 204317.
// Java quirks ported as-is (no observable behavior change): the QUEST_SELECT/SETPRO1/
// CHECK_USER_HAS_QUEST_ITEM fallthrough at 204407, and the SELECT_ACTION_1353/USE_OBJECT branches
// never returning true, all resolve to the same guarded independent branches implemented here.
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

public sealed class _2036ACaptiveFlame : QuestHandlerBase
{
    private const int QuestIdConst = 2036;
    private const int FlameKeeperNpc = 204407;
    private const int MessengerNpc   = 204408;
    private const int LanternObj     = 700236;
    private const int PyreNpc        = 212878;
    private const int LanternItem    = 182204014;

    private readonly IItemDao _itemDao;

    public _2036ACaptiveFlame(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(PyreNpc).OnKill.Add(QuestId);
        foreach (int npc in new[] { FlameKeeperNpc, MessengerNpc, LanternObj, 204317 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, 2035, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != PyreNpc || entry.GetVar(0) != 2) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
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
            if (targetId == FlameKeeperNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return var == 0 && await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return var == 4 && await CheckQuestItemsAsync(env, conn, _itemDao, 4, 4, reward: true, 10000, 10001, ct);
                return false;
            }

            if (targetId == MessengerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    await PlayQuestMovieAsync(conn, player, 79, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return var == 1 && await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == LanternObj)
            {
                if (dialog == DialogAction.USE_OBJECT &&
                    (player.Inventory.FindByItemId(LanternItem)?.Count ?? 0) == 0)
                    await GiveQuestItemAsync(player, conn, _itemDao, LanternItem, 1, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 204317)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
