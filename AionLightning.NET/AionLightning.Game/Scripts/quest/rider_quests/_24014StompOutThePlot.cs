// Port of Java data/scripts/system/handlers/quest/rider_quests/_24014StompOutThePlot.java (pralinka).
// Zone-mission sub-quest of 24010: talk to 203665 (var0 0->1), talk to 203668 (var0 1->2), kill 3
// mobs (210562/216914, var0 2->5), collect-check at 203668 (var0 5->reward, dialogs 5/2120).
// Java bug: onDialogEvent's switch on 203668 had no break after SETPRO2, so a stray SETPRO2 sent
// while var0 != 1 fell through into CHECK_USER_HAS_QUEST_ITEM's body and ran the collect-item check
// (and its item removal / reward flip on success) even though the actual dialog wasn't
// CHECK_USER_HAS_QUEST_ITEM. Fixed here so the check only runs on its own actual dialog id.
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

public sealed class _24014StompOutThePlot : QuestHandlerBase
{
    private const int QuestIdConst = 24014;
    private const int FirstNpc  = 203665;
    private const int SecondNpc = 203668;
    private static readonly int[] _mobs = [210562, 216914];

    private readonly IItemDao _itemDao;

    public _24014StompOutThePlot(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24010, isZoneMission: true, ct);

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
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    if (var != 1) return false;
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 5, reward: true, checkOkId: 5, checkFailId: 2120, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SecondNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        return await DefaultOnKillEventAsync(env, conn, _mobs, 2, 5, ct);
    }
}
