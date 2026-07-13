// Port of Java data/scripts/system/handlers/quest/rider_quests/_24062ADarkPlan.java (pralinka).
// Zone-mission sub-quest of 24061: Merhen (799364, var0 0->1), kill 6 named mobs (var0 2->8),
// Ortiz (799295, var0 1->2 then 8->9), Rhonnam (799326, var0 9->10), entering the sensory-area zone
// at var0==10 advances to 11, Merhen checks the collect items and flips to REWARD (dialog 5); turn in
// at Merhen (SELECT_QUEST_REWARD / SELECTED_QUEST_REWARD1 / SELECTED_QUEST_REWARD2 all finish it).
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

namespace Quest.RiderQuests;

public sealed class _24062ADarkPlan : QuestHandlerBase
{
    private const int QuestIdConst = 24062;
    private const int MerhenNpc  = 799364;
    private const int OrtizNpc   = 799295;
    private const int RhonnamNpc = 799326;
    private const string SensoryZone = "LF4_SensoryArea_Q24062_220070000";
    private static readonly int[] _mobIds = [216061, 216062, 216055, 216056, 216757, 216067];

    private readonly IItemDao _itemDao;

    public _24062ADarkPlan(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MerhenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OrtizNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RhonnamNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SensoryZone);
        foreach (int mob in _mobIds) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24061, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, _mobIds, 2, 8, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SensoryZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 10) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 11, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MerhenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 11)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 11, 11, reward: true, checkOkId: 5, checkFailId: 0, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == OrtizNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 8) return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO9) return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
                return false;
            }
            if (targetId == RhonnamNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 9) return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                if (dialog == DialogAction.SETPRO10) return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MerhenNpc)
            {
                if (dialog is DialogAction.SELECT_QUEST_REWARD or DialogAction.SELECTED_QUEST_REWARD1 or DialogAction.SELECTED_QUEST_REWARD2)
                    return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
