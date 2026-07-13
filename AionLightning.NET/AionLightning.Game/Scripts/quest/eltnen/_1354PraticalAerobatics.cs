// Port of Java data/scripts/system/handlers/quest/eltnen/_1354PraticalAerobatics.java (Ritsu).
// Start/turn-in NPC 203983 (Daedalus). Accept, then SETPRO1 starts a 120s timer and sets var0 0->1;
// fly through 7 Eracus Temple rings advancing var0 1..7, the 7th ring sets var0=8 and flips REWARD +
// ends the timer; turn in at 203983 (var==8 -> dialog 2375). Timer expiry resets var0 to 0 (retry).
// Java switch fallthroughs preserved via accumulated guarded ifs (see inline comments).
// Skips vs Java (state transitions preserved): QuestService.questTimerEnd active-cancel is a no-op here
//   (no timer-cancellation API); the timer callback still resets var0 only while the quest is active.
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

namespace Quest.Eltnen;

public sealed class _1354PraticalAerobatics : QuestHandlerBase
{
    private const int QuestIdConst = 1354;
    private const int Daedalus = 203983;

    private static readonly string[] Rings =
    {
        "ERACUS_TEMPLE_210020000_1", "ERACUS_TEMPLE_210020000_2", "ERACUS_TEMPLE_210020000_3", "ERACUS_TEMPLE_210020000_4",
        "ERACUS_TEMPLE_210020000_5", "ERACUS_TEMPLE_210020000_6", "ERACUS_TEMPLE_210020000_7"
    };

    public _1354PraticalAerobatics(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Daedalus).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Daedalus).OnTalk.Add(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        foreach (string ring in Rings)
            RegisterOnPassFlyingRing(engine, ring);
    }

    public override async ValueTask<bool> OnPassFlyingRingAsync(QuestEnv env, string ringName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (ringName == Rings[0]) { await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct); return true; }
        if (ringName == Rings[1]) { await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct); return true; }
        if (ringName == Rings[2]) { await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct); return true; }
        if (ringName == Rings[3]) { await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct); return true; }
        if (ringName == Rings[4]) { await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct); return true; }
        if (ringName == Rings[5]) { await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct); return true; }
        if (ringName == Rings[6])
        {
            entry.SetVar(0, 8); // Java qs.setQuestVarById(0, 8)
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: true, ct);
            // note: Java QuestService.questTimerEnd active-cancel dropped; the timer callback only resets an active quest.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        entry.SetVar(0, 0);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Daedalus)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Daedalus)
            {
                int var = entry.GetVar(0);
                // Java switch fallthroughs (no breaks): QUEST_SELECT -> SETPRO1 -> SELECT_QUEST_REWARD.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                    if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                {
                    if (var == 0)
                    {
                        StartQuestTimer(env, conn, 120);
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct); // var 0 -> 1
                    }
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1 || dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestEndDialogAsync(env, conn, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Daedalus)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
