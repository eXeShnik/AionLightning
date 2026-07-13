// Port of Java data/scripts/system/handlers/quest/rider_quests/_14062IdelingDragonbound.java (pralinka).
// Zone-mission sub-quest of 14061: talk to Nydrea (799053, var0 0->1), Honeus (799029, var0 1->2 then
// 8->9), Gelon (798979, var0 9->10), kill 5 named mobs (var0 2->8 span), enter the sensory zone
// (var0 10->11), collect-check back at Nydrea (var0 11 stays, reward flip), turn in at Nydrea.
// Java quirk: the mob_ids array lists npc 215664 twice; Java's addOnKillEvent silently dedupes
// registrations per npc/quest pair, but this port's OnKill list has no such guard, so the duplicate
// is dropped here to avoid double-dispatching OnKillAsync for that npc's kills.
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

public sealed class _14062IdelingDragonbound : QuestHandlerBase
{
    private const int QuestIdConst = 14062;
    private const int NydreaNpc = 799053;
    private const int HoneusNpc = 799029;
    private const int GelonNpc  = 798979;
    private const string SensoryAreaZone = "LF4_SensoryArea_Q14062_210050000";

    private static readonly int[] _mobIds = [215661, 215662, 215664, 215666];

    private readonly IItemDao _itemDao;

    public _14062IdelingDragonbound(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NydreaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HoneusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GelonNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SensoryAreaZone);
        foreach (int mob in _mobIds) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 14061, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, _mobIds, 2, 8, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SensoryAreaZone) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 10) return false;

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
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == NydreaNpc)
            {
                if (var == 0 && dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 11 && dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 11, 11, reward: true, checkOkId: 10000, checkFailId: 10001, ct);
                return false;
            }
            if (targetId == HoneusNpc)
            {
                if (var == 1 && dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 8 && dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
                return false;
            }
            if (targetId == GelonNpc)
            {
                if (var == 9 && dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == NydreaNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 2175, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
